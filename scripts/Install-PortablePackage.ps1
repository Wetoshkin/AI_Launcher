[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ZipPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Destination
)

$ErrorActionPreference = 'Stop'

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

function Get-ExpectedSha256 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Sha256Path
    )

    $content = Get-Content -LiteralPath $Sha256Path -Raw
    $match = [regex]::Match($content, '(?im)\b([0-9a-f]{64})\b')

    if (-not $match.Success) {
        throw "SHA256 file does not contain a 64-character hex hash: $Sha256Path"
    }

    return $match.Groups[1].Value.ToLowerInvariant()
}

$resolvedZipPath = Resolve-RequiredPath -Path $ZipPath -Label 'Zip package'
$zipItem = Get-Item -LiteralPath $resolvedZipPath

if ($zipItem.Extension -ne '.zip') {
    throw "Expected a .zip package, got: $resolvedZipPath"
}

$sha256Path = "$resolvedZipPath.sha256"

if (Test-Path -LiteralPath $sha256Path) {
    $expectedHash = Get-ExpectedSha256 -Sha256Path $sha256Path
    $actualHash = (Get-FileHash -LiteralPath $resolvedZipPath -Algorithm SHA256).Hash.ToLowerInvariant()

    if ($actualHash -ne $expectedHash) {
        throw "SHA256 mismatch for $resolvedZipPath. Expected $expectedHash, got $actualHash."
    }

    Write-Host "SHA256 OK: $actualHash"
}
else {
    Write-Warning "SHA256 file not found next to zip; skipping checksum verification: $sha256Path"
}

$destinationPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)
New-Item -ItemType Directory -Force -Path $destinationPath | Out-Null

Write-Host "Extracting to: $destinationPath"
Expand-Archive -LiteralPath $resolvedZipPath -DestinationPath $destinationPath -Force

$exe = Get-ChildItem -LiteralPath $destinationPath -Filter 'Launcher.Desktop.exe' -Recurse -File |
    Select-Object -First 1

if ($null -eq $exe) {
    throw "Launcher.Desktop.exe was not found after extraction: $destinationPath"
}

Write-Host "Installed portable AI Launcher Studio."
Write-Host "Executable:"
Write-Host $exe.FullName
