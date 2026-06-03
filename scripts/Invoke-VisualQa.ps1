param(
    [string]$ExecutablePath = "",
    [string]$OutputRoot = "TestResults\visual-qa",
    [int]$DelaySeconds = 6,
    [string[]]$WindowSizes = @(),
    [switch]$NoLaunch,
    [switch]$ChecklistOnly,
    [switch]$CloseAfterCapture
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepoPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path -Path (Get-Location) -ChildPath $Path
}

function Find-PublishExecutable {
    $candidates = @(
        "publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe",
        "src\Launcher.Desktop\bin\Release\net8.0\win-x64\Launcher.Desktop.exe",
        "src\Launcher.Desktop\bin\Debug\net8.0\Launcher.Desktop.exe"
    )

    foreach ($candidate in $candidates) {
        $resolved = Resolve-RepoPath $candidate
        if (Test-Path $resolved) {
            return $resolved
        }
    }

    return $null
}

function Ensure-NativeWindowApi {
    try {
        [VisualQaNative.WindowApi] | Out-Null
        return
    }
    catch {
        Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

namespace VisualQaNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static class WindowApi
    {
        public const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rect rect);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);
    }
}
"@
    }
}

function Get-ProcessMainWindowHandle {
    param([System.Diagnostics.Process]$Process)

    if (-not $Process) {
        return [IntPtr]::Zero
    }

    try {
        $Process.Refresh()
        return $Process.MainWindowHandle
    }
    catch {
        return [IntPtr]::Zero
    }
}

function Show-ProcessMainWindow {
    param([System.Diagnostics.Process]$Process)

    $handle = Get-ProcessMainWindowHandle -Process $Process
    if ($handle -eq [IntPtr]::Zero) {
        return $false
    }

    Ensure-NativeWindowApi
    [VisualQaNative.WindowApi]::ShowWindow($handle, [VisualQaNative.WindowApi]::SW_RESTORE) | Out-Null
    [VisualQaNative.WindowApi]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Milliseconds 800
    return $true
}

function Get-ProcessMainWindowBounds {
    param([System.Diagnostics.Process]$Process)

    $handle = Get-ProcessMainWindowHandle -Process $Process
    if ($handle -eq [IntPtr]::Zero) {
        return $null
    }

    Ensure-NativeWindowApi

    $rect = New-Object VisualQaNative.Rect
    $ok = [VisualQaNative.WindowApi]::GetWindowRect($handle, [ref]$rect)
    if (-not $ok) {
        return $null
    }

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    if ($width -le 0 -or $height -le 0) {
        return $null
    }

    Add-Type -AssemblyName System.Drawing
    return [System.Drawing.Rectangle]::FromLTRB($rect.Left, $rect.Top, $rect.Right, $rect.Bottom)
}

function Set-ProcessMainWindowSize {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$Width,
        [int]$Height
    )

    $handle = Get-ProcessMainWindowHandle -Process $Process
    if ($handle -eq [IntPtr]::Zero) {
        return $false
    }

    $bounds = Get-ProcessMainWindowBounds -Process $Process
    if (-not $bounds) {
        return $false
    }

    Ensure-NativeWindowApi
    [VisualQaNative.WindowApi]::MoveWindow($handle, $bounds.Left, $bounds.Top, $Width, $Height, $true) | Out-Null
    Start-Sleep -Milliseconds 800
    return $true
}

function Convert-WindowSize {
    param([string]$Size)

    $match = [regex]::Match($Size, '^\s*(\d{3,5})\s*x\s*(\d{3,5})\s*$')
    if (-not $match.Success) {
        throw "Некорректный размер окна: $Size. Используйте формат 1280x720."
    }

    return [pscustomobject]@{
        Label = "$($match.Groups[1].Value)x$($match.Groups[2].Value)"
        Width = [int]$match.Groups[1].Value
        Height = [int]$match.Groups[2].Value
    }
}

function Save-BoundsScreenshot {
    param(
        [System.Drawing.Rectangle]$Bounds,
        [string]$ScreenshotPath
    )

    Add-Type -AssemblyName System.Drawing

    $bitmap = New-Object System.Drawing.Bitmap $Bounds.Width, $Bounds.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.CopyFromScreen($Bounds.Left, $Bounds.Top, 0, 0, $Bounds.Size)
        $bitmap.Save($ScreenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Save-WindowPrintScreenshot {
    param(
        [System.Diagnostics.Process]$Process,
        [System.Drawing.Rectangle]$Bounds,
        [string]$ScreenshotPath
    )

    $handle = Get-ProcessMainWindowHandle -Process $Process
    if ($handle -eq [IntPtr]::Zero) {
        return $false
    }

    Ensure-NativeWindowApi
    Add-Type -AssemblyName System.Drawing

    $bitmap = New-Object System.Drawing.Bitmap $Bounds.Width, $Bounds.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $hdc = [IntPtr]::Zero

    try {
        $hdc = $graphics.GetHdc()
        $ok = [VisualQaNative.WindowApi]::PrintWindow($handle, $hdc, 2)
    }
    finally {
        if ($hdc -ne [IntPtr]::Zero) {
            $graphics.ReleaseHdc($hdc)
        }

        $graphics.Dispose()
    }

    if (-not $ok) {
        $bitmap.Dispose()
        return $false
    }

    try {
        $bitmap.Save($ScreenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
        return $true
    }
    finally {
        $bitmap.Dispose()
    }
}

function Save-WindowScreenshot {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$ScreenshotPath
    )

    if (-not (Show-ProcessMainWindow -Process $Process)) {
        return $false
    }

    $bounds = Get-ProcessMainWindowBounds -Process $Process
    if (-not $bounds) {
        return $false
    }

    if (Save-WindowPrintScreenshot -Process $Process -Bounds $bounds -ScreenshotPath $ScreenshotPath) {
        return $true
    }

    Save-BoundsScreenshot -Bounds $bounds -ScreenshotPath $ScreenshotPath
    return $true
}

function Save-FullscreenScreenshot {
    param([string]$ScreenshotPath)

    Add-Type -AssemblyName System.Windows.Forms

    $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
    Save-BoundsScreenshot -Bounds $bounds -ScreenshotPath $ScreenshotPath
}

function New-VisualQaChecklist {
    param(
        [string]$RunDirectory,
        [string[]]$ScreenshotFileNames,
        [string]$CaptureMode
    )

    $checklistPath = Join-Path $RunDirectory "visual-qa-checklist.md"
    $screenshotLine = if (-not $ScreenshotFileNames -or $ScreenshotFileNames.Count -eq 0) {
        "- Screenshot: не создавался"
    }
    else {
        (($ScreenshotFileNames | ForEach-Object { "- Screenshot: ``$_``" }) -join [Environment]::NewLine)
    }

    @"
# AI Launcher Studio visual QA smoke

- Дата: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
$screenshotLine
- Capture mode: $CaptureMode

## Чеклист

- [ ] GUI открывается как рабочий продукт, без landing-заглушки.
- [ ] Тема светлая, теплая, оранжевая; темные панели не вернулись.
- [ ] Все основные labels и кнопки на русском языке.
- [ ] Кнопки выбора папок/путей понятны.
- [ ] Карточки режима выглядят как важный выбор.
- [ ] Очередь скачивания HF видна и читается.
- [ ] Длинные пути не ломают layout.
- [ ] Статусы и ошибки не перекрывают соседние элементы.
- [ ] На `1280x720` нет критичных переполнений.

## Заметки

-
"@ | Set-Content -Path $checklistPath -Encoding UTF8

    return $checklistPath
}

$runName = Get-Date -Format "yyyyMMdd-HHmmss"
$runDirectory = Join-Path (Resolve-RepoPath $OutputRoot) $runName
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null

$process = $null
if (-not $NoLaunch -and -not $ChecklistOnly) {
    $resolvedExecutable = if ([string]::IsNullOrWhiteSpace($ExecutablePath)) {
        Find-PublishExecutable
    }
    else {
        Resolve-RepoPath $ExecutablePath
    }

    if (-not $resolvedExecutable -or -not (Test-Path $resolvedExecutable)) {
        throw "Launcher.Desktop.exe не найден. Соберите проект или передайте -ExecutablePath."
    }

    $process = Start-Process -FilePath $resolvedExecutable -PassThru
    Write-Host "GUI запущен, PID $($process.Id). Жду $DelaySeconds секунд перед screenshot."
    Start-Sleep -Seconds $DelaySeconds
}

$screenshotNames = @()
$captureMode = "none"
if (-not $ChecklistOnly) {
    $sizes = @($WindowSizes | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { Convert-WindowSize $_ })
    if ($sizes.Count -gt 0 -and -not $process) {
        throw "-WindowSizes требует запущенный GUI. Уберите -NoLaunch или -ChecklistOnly."
    }

    if ($sizes.Count -gt 0) {
        foreach ($size in $sizes) {
            if (-not (Set-ProcessMainWindowSize -Process $process -Width $size.Width -Height $size.Height)) {
                throw "Не удалось изменить размер окна до $($size.Label)."
            }

            $windowScreenshotName = "launcher-window-$($size.Label).png"
            $windowScreenshotPath = Join-Path $runDirectory $windowScreenshotName
            if (-not (Save-WindowScreenshot -Process $process -ScreenshotPath $windowScreenshotPath)) {
                throw "Не удалось сохранить screenshot окна для размера $($size.Label)."
            }

            $screenshotNames += $windowScreenshotName
            Write-Host "Screenshot окна $($size.Label) сохранен: $windowScreenshotPath"
        }

        $captureMode = "window matrix"
    }
    else {
        $windowScreenshotName = "launcher-window.png"
        $windowScreenshotPath = Join-Path $runDirectory $windowScreenshotName

        if (Save-WindowScreenshot -Process $process -ScreenshotPath $windowScreenshotPath) {
            $screenshotNames += $windowScreenshotName
            $captureMode = "window"
            Write-Host "Screenshot окна сохранен: $windowScreenshotPath"
        }
        else {
            $screenshotName = "launcher-fullscreen.png"
            $captureMode = "fullscreen fallback"
            $fullscreenScreenshotPath = Join-Path $runDirectory $screenshotName
            Save-FullscreenScreenshot -ScreenshotPath $fullscreenScreenshotPath
            $screenshotNames += $screenshotName
            Write-Host "MainWindowHandle недоступен, сохранен fullscreen screenshot: $fullscreenScreenshotPath"
        }
    }
}

$checklistPath = New-VisualQaChecklist -RunDirectory $runDirectory -ScreenshotFileNames $screenshotNames -CaptureMode $captureMode
Write-Host "Visual QA output: $runDirectory"
Write-Host "Checklist: $checklistPath"

if ($process -and $CloseAfterCapture) {
    Stop-Process -Id $process.Id -Force
    Wait-Process -Id $process.Id -Timeout 10 -ErrorAction SilentlyContinue
    Write-Host "GUI закрыт после screenshot."
}
elseif ($process) {
    Write-Host "Закройте GUI вручную после проверки."
}
