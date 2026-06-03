[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$RuntimePath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ModelPath,

    [ValidateNotNullOrEmpty()]
    [string]$ProviderModel = "",

    [ValidateRange(1, 65535)]
    [int]$Port = 18084,

    [ValidateRange(1024, 1048576)]
    [int]$ContextTokens = 16384,

    [int]$GpuLayers = 0,

    [ValidateRange(10, 600)]
    [int]$TimeoutSeconds = 120,

    [ValidateNotNullOrEmpty()]
    [string]$ExpectedText = "OK"
)

$ErrorActionPreference = "Stop"

function Resolve-RequiredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label not found: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Test-TcpPortOpen {
    param(
        [Parameter(Mandatory = $true)]
        [string]$HostName,

        [Parameter(Mandatory = $true)]
        [int]$Port
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connectTask = $client.ConnectAsync($HostName, $Port)
        if (-not $connectTask.Wait(500)) {
            return $false
        }

        return $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Get-OpenCodeCommand {
    $command = Get-Command opencode.cmd -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $command) {
        $command = Get-Command opencode -ErrorAction SilentlyContinue | Select-Object -First 1
    }

    if (-not $command) {
        throw "opencode not found in PATH."
    }

    return $command.Source
}

function Get-DefaultProviderModel {
    param([string]$ModelPath)

    return "local/" + [System.IO.Path]::GetFileName($ModelPath)
}

function Get-LocalModelKey {
    param([string]$ProviderModel)

    $prefix = "local/"
    if ($ProviderModel.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        return $ProviderModel.Substring($prefix.Length)
    }

    return $ProviderModel
}

function Write-LogTail {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Title,

        [int]$Tail = 40
    )

    Write-Host "--- $Title ---"
    if (Test-Path -LiteralPath $Path) {
        Get-Content -LiteralPath $Path -Tail $Tail -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "log file was not created"
    }
}

$resolvedRuntimePath = Resolve-RequiredPath -Path $RuntimePath -Label "Runtime"
$resolvedModelPath = Resolve-RequiredPath -Path $ModelPath -Label "Model"
$effectiveProviderModel = if ([string]::IsNullOrWhiteSpace($ProviderModel)) {
    Get-DefaultProviderModel -ModelPath $resolvedModelPath
}
else {
    $ProviderModel
}
$localModelKey = Get-LocalModelKey -ProviderModel $effectiveProviderModel
$opencodePath = Get-OpenCodeCommand
$runRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ai-launcher-opencode-smoke-" + [guid]::NewGuid().ToString("N"))
$projectRoot = Join-Path $runRoot "project"
$logRoot = Join-Path $runRoot "logs"
$llamaStdout = Join-Path $logRoot "llama-stdout.log"
$llamaStderr = Join-Path $logRoot "llama-stderr.log"
$opencodeStdout = Join-Path $logRoot "opencode-stdout.log"
$opencodeStderr = Join-Path $logRoot "opencode-stderr.log"
$llamaProcess = $null
$opencodeProcess = $null
$success = $false

New-Item -ItemType Directory -Force -Path $projectRoot, $logRoot | Out-Null

try {
    if (Test-TcpPortOpen -HostName "127.0.0.1" -Port $Port) {
        throw "OpenCode smoke cannot start: port $Port is already occupied."
    }

    $config = @{
        provider = @{
            local = @{
                npm = "@ai-sdk/openai-compatible"
                name = "local"
                options = @{
                    baseURL = "http://127.0.0.1:$Port/v1"
                    apiKey = "local"
                }
                models = @{
                    $localModelKey = @{
                        tools = @{
                            disabled = $true
                        }
                    }
                }
            }
        }
        model = $effectiveProviderModel
    } | ConvertTo-Json -Depth 10
    Set-Content -LiteralPath (Join-Path $projectRoot "opencode.json") -Value $config -Encoding UTF8

    Write-Host "Starting OpenCode smoke:"
    Write-Host "Runtime:       $resolvedRuntimePath"
    Write-Host "Model:         $resolvedModelPath"
    Write-Host "ProviderModel: $effectiveProviderModel"
    Write-Host "Context:       $ContextTokens"

    $llamaProcess = Start-Process `
        -FilePath $resolvedRuntimePath `
        -ArgumentList @("-m", $resolvedModelPath, "--host", "127.0.0.1", "--port", [string]$Port, "-ngl", [string]$GpuLayers, "-c", [string]$ContextTokens) `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $llamaStdout `
        -RedirectStandardError $llamaStderr

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $ready = $false
    while ((Get-Date) -lt $deadline) {
        if ($llamaProcess.HasExited) {
            break
        }

        try {
            $response = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/v1/models" -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $ready) {
        throw "llama-server did not become ready within $TimeoutSeconds seconds."
    }

    $env:OPENAI_BASE_URL = "http://127.0.0.1:$Port/v1"
    $env:OPENAI_API_KEY = "local"
    $opencodeProcess = Start-Process `
        -FilePath $opencodePath `
        -ArgumentList @("run", "--pure", "--dir", $projectRoot, "--model", $effectiveProviderModel, "--format", "json", "Reply exactly with $ExpectedText and nothing else.") `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $opencodeStdout `
        -RedirectStandardError $opencodeStderr

    $finished = Wait-Process -Id $opencodeProcess.Id -Timeout $TimeoutSeconds -ErrorAction SilentlyContinue
    if (-not $finished -and -not $opencodeProcess.HasExited) {
        throw "opencode did not exit within $TimeoutSeconds seconds."
    }

    $stdout = Get-Content -LiteralPath $opencodeStdout -Raw -ErrorAction SilentlyContinue
    $stderr = Get-Content -LiteralPath $opencodeStderr -Raw -ErrorAction SilentlyContinue
    if ($opencodeProcess.ExitCode -ne 0) {
        throw "opencode exited with $($opencodeProcess.ExitCode)."
    }

    if (($stdout + $stderr) -notmatch [regex]::Escape($ExpectedText)) {
        throw "opencode output did not contain expected text: $ExpectedText"
    }

    Write-Host "OPENCODE_AGENT_SMOKE_OK"
    (($stdout + $stderr) -split "`n" | Select-Object -Last 20)
    $success = $true
}
catch {
    Write-Host "OPENCODE_AGENT_SMOKE_FAILED"
    Write-Host $_.Exception.Message
    Write-LogTail -Path $opencodeStdout -Title "opencode stdout tail"
    Write-LogTail -Path $opencodeStderr -Title "opencode stderr tail"
    Write-LogTail -Path $llamaStderr -Title "llama stderr tail" -Tail 80
}
finally {
    Remove-Item Env:\OPENAI_BASE_URL -ErrorAction SilentlyContinue
    Remove-Item Env:\OPENAI_API_KEY -ErrorAction SilentlyContinue

    if ($opencodeProcess -and -not $opencodeProcess.HasExited) {
        Stop-Process -Id $opencodeProcess.Id -Force -ErrorAction SilentlyContinue
    }

    if ($llamaProcess -and -not $llamaProcess.HasExited) {
        Stop-Process -Id $llamaProcess.Id -Force -ErrorAction SilentlyContinue
        Wait-Process -Id $llamaProcess.Id -Timeout 10 -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $runRoot) {
        Remove-Item -LiteralPath $runRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($success) {
    exit 0
}

exit 1
