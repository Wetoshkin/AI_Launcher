[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()]
    [string]$RuntimePath,

    [ValidateNotNullOrEmpty()]
    [string]$ModelsRoot,

    [ValidateNotNullOrEmpty()]
    [string]$ProjectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path,

    [ValidateSet("opencode", "kilo", "claw", "aider", "all")]
    [string]$RequiredAgent = "all",

    [ValidateRange(0, 1024)]
    [double]$MinModelSizeGb = 0.2
)

$ErrorActionPreference = "Stop"

function Resolve-OptionalExistingPath {
    param(
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Add-ExistingDirectory {
    param(
        [System.Collections.Generic.List[string]]$Directories,
        [string]$Path
    )

    $resolved = Resolve-OptionalExistingPath -Path $Path
    if ($resolved -and (Test-Path -LiteralPath $resolved -PathType Container) -and -not $Directories.Contains($resolved)) {
        $Directories.Add($resolved) | Out-Null
    }
}

function Find-RuntimeCandidates {
    param(
        [string]$RuntimePath,
        [string]$ProjectRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($RuntimePath)) {
        $resolvedRuntime = Resolve-OptionalExistingPath -Path $RuntimePath
        if ($resolvedRuntime -and (Test-Path -LiteralPath $resolvedRuntime -PathType Leaf)) {
            return @($resolvedRuntime)
        }

        return @()
    }

    $directories = [System.Collections.Generic.List[string]]::new()
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $ProjectRoot "runtimes")
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $ProjectRoot "publish")
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $ProjectRoot "bin")
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $ProjectRoot "tools")
    Add-ExistingDirectory -Directories $directories -Path "D:\AI\AI launcher\runtimes"
    Add-ExistingDirectory -Directories $directories -Path "D:\AI\AI-Launcher-Studio\runtimes"

    $candidates = foreach ($directory in $directories) {
        Get-ChildItem -LiteralPath $directory -Recurse -File -Filter "llama-server*.exe" -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty FullName
    }

    return @($candidates | Sort-Object -Unique)
}

function Get-DefaultModelRoots {
    param(
        [string]$ModelsRoot,
        [string]$ProjectRoot
    )

    $directories = [System.Collections.Generic.List[string]]::new()

    if (-not [string]::IsNullOrWhiteSpace($ModelsRoot)) {
        Add-ExistingDirectory -Directories $directories -Path $ModelsRoot
        return $directories.ToArray()
    }

    Add-ExistingDirectory -Directories $directories -Path (Join-Path $ProjectRoot "models")
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $ProjectRoot "Models")
    Add-ExistingDirectory -Directories $directories -Path "D:\AI\Models"
    Add-ExistingDirectory -Directories $directories -Path "D:\AI\models"
    Add-ExistingDirectory -Directories $directories -Path "D:\AI\AI launcher\models"
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $HOME ".lmstudio\models")
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $HOME ".lmstudio\.internal\bundled-models")
    Add-ExistingDirectory -Directories $directories -Path (Join-Path $HOME ".cache\lm-studio\models")

    return $directories.ToArray()
}

function Test-EmbeddingOnlyName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalized = $Path.ToLowerInvariant()
    return ($normalized -match "nomic[-_ ]?embed" -or
        $normalized -match "(^|[\\/_. -])bge([\\/_. -]|$)" -or
        $normalized -match "(^|[\\/_. -])e5([\\/_. -]|$)" -or
        $normalized -match "(^|[\\/_. -])embedding(s)?([\\/_. -]|$)" -or
        $normalized -match "(^|[\\/_. -])embed([\\/_. -]|$)")
}

function Find-GgufModels {
    param(
        [string[]]$Roots,
        [double]$MinModelSizeGb
    )

    $minBytes = [int64]($MinModelSizeGb * 1GB)
    $models = foreach ($root in $Roots) {
        Get-ChildItem -LiteralPath $root -Recurse -File -Filter "*.gguf" -ErrorAction SilentlyContinue | ForEach-Object {
            $isEmbedding = Test-EmbeddingOnlyName -Path $_.FullName
            [pscustomobject]@{
                Path = $_.FullName
                SizeGb = [math]::Round($_.Length / 1GB, 3)
                IsEmbeddingOnly = $isEmbedding
                MeetsMinSize = $_.Length -ge $minBytes
                LooksLikeChatOrCoding = (-not $isEmbedding -and $_.Length -ge $minBytes)
            }
        }
    }

    return @($models | Sort-Object -Property LooksLikeChatOrCoding, SizeGb, Path -Descending)
}

function Get-RequiredAgents {
    param(
        [string]$RequiredAgent
    )

    if ($RequiredAgent -eq "all") {
        return @("opencode", "kilo", "claw", "aider")
    }

    return @($RequiredAgent)
}

function Get-AgentVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command
    )

    $versionArguments = @("--version", "version", "-v")
    foreach ($argument in $versionArguments) {
        $job = $null
        try {
            $job = Start-Job -ScriptBlock {
                param($CommandName, $VersionArgument)
                & $CommandName $VersionArgument 2>&1 | Select-Object -First 5
            } -ArgumentList $Command, $argument

            $completed = Wait-Job -Job $job -Timeout 5
            if (-not $completed) {
                Stop-Job -Job $job -ErrorAction SilentlyContinue
                continue
            }

            $output = Receive-Job -Job $job -ErrorAction SilentlyContinue
            if ($output) {
                return (($output | ForEach-Object { $_.ToString().Trim() }) -join " ").Trim()
            }
        }
        catch {
            continue
        }
        finally {
            if ($job) {
                Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
            }
        }
    }

    return $null
}

function Test-AgentCli {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command
    )

    $resolved = Get-Command -Name $Command -ErrorAction SilentlyContinue
    if (-not $resolved) {
        return [pscustomobject]@{
            Name = $Command
            Available = $false
            Path = $null
            Version = $null
        }
    }

    return [pscustomobject]@{
        Name = $Command
        Available = $true
        Path = $resolved.Source
        Version = Get-AgentVersion -Command $Command
    }
}

$resolvedProjectRoot = Resolve-OptionalExistingPath -Path $ProjectRoot
if (-not $resolvedProjectRoot -or -not (Test-Path -LiteralPath $resolvedProjectRoot -PathType Container)) {
    throw "ProjectRoot not found: $ProjectRoot"
}

$blockers = [System.Collections.Generic.List[string]]::new()

Write-Host "Agent E2E readiness check"
Write-Host "ProjectRoot:      $resolvedProjectRoot"
Write-Host "RequiredAgent:    $RequiredAgent"
Write-Host "MinModelSizeGb:   $MinModelSizeGb"

$runtimeCandidates = Find-RuntimeCandidates -RuntimePath $RuntimePath -ProjectRoot $resolvedProjectRoot
if ($RuntimePath) {
    Write-Host "RuntimePath:      $RuntimePath"
}
else {
    Write-Host "RuntimePath:      auto-discovery"
}

if ($runtimeCandidates.Count -eq 0) {
    $blockers.Add("runtime: llama-server*.exe не найден. Передайте -RuntimePath или установите runtime в очевидное место.") | Out-Null
    Write-Host "Runtime:          BLOCKED"
}
else {
    Write-Host "Runtime:          OK"
    $runtimeCandidates | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" }
}

$modelRoots = Get-DefaultModelRoots -ModelsRoot $ModelsRoot -ProjectRoot $resolvedProjectRoot
if ($ModelsRoot) {
    Write-Host "ModelsRoot:       $ModelsRoot"
}
else {
    Write-Host "ModelsRoot:       auto-discovery"
}

if ($modelRoots.Count -eq 0) {
    $blockers.Add("models: директории с GGUF не найдены. Передайте -ModelsRoot.") | Out-Null
    $models = @()
}
else {
    Write-Host "Model roots:"
    $modelRoots | ForEach-Object { Write-Host "  $_" }
    $models = Find-GgufModels -Roots $modelRoots -MinModelSizeGb $MinModelSizeGb
}

$chatModels = @($models | Where-Object { $_.LooksLikeChatOrCoding })
$embeddingModels = @($models | Where-Object { $_.IsEmbeddingOnly })
$smallNonEmbeddingModels = @($models | Where-Object { -not $_.IsEmbeddingOnly -and -not $_.MeetsMinSize })

Write-Host "GGUF total:       $($models.Count)"
Write-Host "Chat/code GGUF:   $($chatModels.Count)"
if ($chatModels.Count -gt 0) {
    $chatModels | Select-Object -First 10 | ForEach-Object {
        Write-Host ("  OK {0} GB  {1}" -f $_.SizeGb, $_.Path)
    }
}
else {
    $blockers.Add("models: нет вероятной chat/code GGUF >= $MinModelSizeGb GB. Embedding GGUF годится только для runtime smoke, не для настоящего agent E2E.") | Out-Null
}

if ($embeddingModels.Count -gt 0) {
    Write-Host "Embedding-only GGUF:"
    $embeddingModels | Select-Object -First 10 | ForEach-Object {
        Write-Host ("  NOT_AGENT_MODEL {0} GB  {1}" -f $_.SizeGb, $_.Path)
    }
}

if ($smallNonEmbeddingModels.Count -gt 0) {
    Write-Host "Too small for MinModelSizeGb:"
    $smallNonEmbeddingModels | Select-Object -First 10 | ForEach-Object {
        Write-Host ("  TOO_SMALL {0} GB  {1}" -f $_.SizeGb, $_.Path)
    }
}

$requiredAgents = Get-RequiredAgents -RequiredAgent $RequiredAgent
$agentStatuses = $requiredAgents | ForEach-Object { Test-AgentCli -Command $_ }
$missingAgents = @($agentStatuses | Where-Object { -not $_.Available })

Write-Host "Agent CLIs:"
foreach ($agent in $agentStatuses) {
    if ($agent.Available) {
        $version = if ($agent.Version) { $agent.Version } else { "version: no quick response" }
        Write-Host "  OK $($agent.Name): $($agent.Path) ($version)"
    }
    else {
        Write-Host "  BLOCKED $($agent.Name): not found in PATH"
    }
}

foreach ($agent in $missingAgents) {
    $blockers.Add("agent: '$($agent.Name)' не найден в PATH.") | Out-Null
}

if ($blockers.Count -eq 0) {
    Write-Host "AGENT_E2E_READY"
    exit 0
}

Write-Host "AGENT_E2E_BLOCKED"
Write-Host "Blockers:"
foreach ($blocker in $blockers) {
    Write-Host "  - $blocker"
}

exit 1
