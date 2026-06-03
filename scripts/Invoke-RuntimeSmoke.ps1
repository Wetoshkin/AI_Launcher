[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$RuntimePath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ModelPath,

    [Alias("Host")]
    [ValidateNotNullOrEmpty()]
    [string]$HostName = "127.0.0.1",

    [ValidateRange(1, 65535)]
    [int]$Port = 18080,

    [ValidateRange(1, 1048576)]
    [int]$ContextTokens = 512,

    [int]$GpuLayers = 0,

    [ValidateRange(5, 600)]
    [int]$TimeoutSeconds = 90,

    [switch]$Embeddings
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

function Write-LogTail {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [int]$Tail,

        [Parameter(Mandatory = $true)]
        [string]$Title
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
$logDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("ai-launcher-runtime-smoke-" + [guid]::NewGuid().ToString("N"))
$stdoutPath = Join-Path $logDirectory "stdout.log"
$stderrPath = Join-Path $logDirectory "stderr.log"
$process = $null

New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null

try {
    if (Test-TcpPortOpen -HostName $HostName -Port $Port) {
        throw "Runtime smoke cannot start: port $Port on $HostName is already occupied. Choose another -Port or stop the existing server first."
    }

    $arguments = @(
        "-m", $resolvedModelPath,
        "--host", $HostName,
        "--port", [string]$Port,
        "-ngl", [string]$GpuLayers,
        "-c", [string]$ContextTokens
    )
    if ($Embeddings) {
        $arguments += "--embeddings"
    }

    Write-Host "Starting runtime smoke:"
    Write-Host "Runtime: $resolvedRuntimePath"
    Write-Host "Model:   $resolvedModelPath"
    Write-Host "URL:     http://$HostName`:$Port/v1/models"

    $process = Start-Process `
        -FilePath $resolvedRuntimePath `
        -ArgumentList $arguments `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdoutPath `
        -RedirectStandardError $stderrPath

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $modelsResponse = $null
    while ((Get-Date) -lt $deadline) {
        if ($process.HasExited) {
            break
        }

        try {
            $response = Invoke-WebRequest -Uri "http://$HostName`:$Port/v1/models" -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -eq 200) {
                $modelsResponse = $response.Content
                break
            }
        }
        catch {
            Start-Sleep -Seconds 2
        }
    }

    if (-not $modelsResponse) {
        Write-LogTail -Path $stdoutPath -Tail 40 -Title "stdout tail"
        Write-LogTail -Path $stderrPath -Tail 80 -Title "stderr tail"
        throw "Runtime smoke failed: /v1/models did not become ready within $TimeoutSeconds seconds."
    }

    Write-Host "REAL_RUNTIME_SMOKE_OK"
    Write-Host "PID: $($process.Id)"
    Write-Host "Endpoint: http://$HostName`:$Port/v1/models"
    Write-Host $modelsResponse
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue
    }

    if ($process -and -not $process.HasExited) {
        Write-Warning "Runtime process $($process.Id) is still running after stop attempt."
    }

    Start-Sleep -Milliseconds 500
    if (Test-Path -LiteralPath $logDirectory) {
        Remove-Item -LiteralPath $logDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}
