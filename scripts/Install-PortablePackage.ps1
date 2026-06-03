[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ZipPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Destination,

    [switch]$CreateDesktopShortcut,

    [switch]$CreateStartMenuShortcut,

    [ValidateNotNullOrEmpty()]
    [string]$ShortcutName = 'AI Launcher Studio',

    [ValidateNotNullOrEmpty()]
    [string]$StartMenuDirectory
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

function Expand-SafeZipArchive {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArchivePath,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $destinationFullPath = [System.IO.Path]::GetFullPath($DestinationPath)
    if (-not $destinationFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $destinationFullPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrWhiteSpace($entry.FullName)) {
                continue
            }

            if ([System.IO.Path]::IsPathRooted($entry.FullName)) {
                throw "Unsafe rooted zip entry: $($entry.FullName)"
            }

            $targetPath = [System.IO.Path]::GetFullPath((Join-Path $destinationFullPath $entry.FullName))
            if (-not $targetPath.StartsWith($destinationFullPath, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Unsafe zip entry escapes destination: $($entry.FullName)"
            }
        }

        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrWhiteSpace($entry.FullName)) {
                continue
            }

            $targetPath = [System.IO.Path]::GetFullPath((Join-Path $destinationFullPath $entry.FullName))
            if ($entry.FullName.EndsWith('/') -or $entry.FullName.EndsWith('\')) {
                New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
                continue
            }

            $targetDirectory = Split-Path -Parent $targetPath
            New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $targetPath, $true)
        }
    }
    finally {
        $archive.Dispose()
    }
}

function New-Shortcut {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ShortcutPath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    $shortcutDirectory = Split-Path -Parent $ShortcutPath
    New-Item -ItemType Directory -Force -Path $shortcutDirectory | Out-Null

    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($ShortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.Description = 'AI Launcher Studio'
    $shortcut.Save()
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
Expand-SafeZipArchive -ArchivePath $resolvedZipPath -DestinationPath $destinationPath

$exe = Get-ChildItem -LiteralPath $destinationPath -Filter 'Launcher.Desktop.exe' -Recurse -File |
    Select-Object -First 1

if ($null -eq $exe) {
    throw "Launcher.Desktop.exe was not found after extraction: $destinationPath"
}

Write-Host "Installed portable AI Launcher Studio."
Write-Host "Executable:"
Write-Host $exe.FullName

$safeShortcutName = [regex]::Replace($ShortcutName, '[\\/:*?"<>|]', '-').Trim()
if ([string]::IsNullOrWhiteSpace($safeShortcutName)) {
    $safeShortcutName = 'AI Launcher Studio'
}

if ($CreateDesktopShortcut) {
    $desktopPath = [Environment]::GetFolderPath([Environment+SpecialFolder]::DesktopDirectory)
    if ([string]::IsNullOrWhiteSpace($desktopPath)) {
        throw 'Desktop directory could not be resolved.'
    }

    $shortcutPath = Join-Path $desktopPath "$safeShortcutName.lnk"
    New-Shortcut -ShortcutPath $shortcutPath -TargetPath $exe.FullName -WorkingDirectory $exe.DirectoryName
    Write-Host "Desktop shortcut:"
    Write-Host $shortcutPath
}

if ($CreateStartMenuShortcut) {
    $programsPath = if ([string]::IsNullOrWhiteSpace($StartMenuDirectory)) {
        [Environment]::GetFolderPath([Environment+SpecialFolder]::Programs)
    }
    else {
        $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($StartMenuDirectory)
    }

    if ([string]::IsNullOrWhiteSpace($programsPath)) {
        throw 'Start Menu programs directory could not be resolved.'
    }

    $appShortcutDirectory = Join-Path $programsPath 'AI Launcher Studio'
    $shortcutPath = Join-Path $appShortcutDirectory "$safeShortcutName.lnk"
    New-Shortcut -ShortcutPath $shortcutPath -TargetPath $exe.FullName -WorkingDirectory $exe.DirectoryName
    Write-Host "Start Menu shortcut:"
    Write-Host $shortcutPath
}
