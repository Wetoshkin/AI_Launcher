param(
    [string]$ExecutablePath = "",
    [string]$OutputRoot = "TestResults\visual-qa",
    [int]$DelaySeconds = 6,
    [switch]$NoLaunch,
    [switch]$ChecklistOnly
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

function Save-FullscreenScreenshot {
    param([string]$ScreenshotPath)

    Add-Type -AssemblyName System.Windows.Forms
    Add-Type -AssemblyName System.Drawing

    $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
    $bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bounds.Size)
        $bitmap.Save($ScreenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function New-VisualQaChecklist {
    param(
        [string]$RunDirectory,
        [string]$ScreenshotFileName
    )

    $checklistPath = Join-Path $RunDirectory "visual-qa-checklist.md"
    $screenshotLine = if ([string]::IsNullOrWhiteSpace($ScreenshotFileName)) {
        "- Screenshot: не создавался"
    }
    else {
        "- Screenshot: ``$ScreenshotFileName``"
    }

    @"
# AI Launcher Studio visual QA smoke

- Дата: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
$screenshotLine

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

$screenshotName = ""
if (-not $ChecklistOnly) {
    $screenshotName = "launcher-fullscreen.png"
    Save-FullscreenScreenshot -ScreenshotPath (Join-Path $runDirectory $screenshotName)
}

$checklistPath = New-VisualQaChecklist -RunDirectory $runDirectory -ScreenshotFileName $screenshotName
Write-Host "Visual QA output: $runDirectory"
Write-Host "Checklist: $checklistPath"

if ($process) {
    Write-Host "Закройте GUI вручную после проверки."
}
