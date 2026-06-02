@echo off
setlocal
cd /d "%~dp0"

call "%~dp0publish-ai-launcher-studio.bat"
if errorlevel 1 exit /b 1

set "OUT_DIR=%~dp0publish\AI-Launcher-Studio-win-x64"
set "ZIP_PATH=%~dp0publish\AI-Launcher-Studio-win-x64.zip"
set "HASH_PATH=%ZIP_PATH%.sha256"

if exist "%ZIP_PATH%" del "%ZIP_PATH%"
if exist "%HASH_PATH%" del "%HASH_PATH%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Compress-Archive -Path '%OUT_DIR%\*' -DestinationPath '%ZIP_PATH%' -Force"

if errorlevel 1 (
  echo.
  echo Package failed.
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$ErrorActionPreference = 'Stop'; $stream = [System.IO.File]::OpenRead('%ZIP_PATH%'); try { $sha256 = [System.Security.Cryptography.SHA256]::Create(); try { $hashText = -join ($sha256.ComputeHash($stream) | ForEach-Object { $_.ToString('x2') }) } finally { $sha256.Dispose() } } finally { $stream.Dispose() }; $line = ('{0}  {1}' -f $hashText, (Split-Path -Leaf '%ZIP_PATH%')); Set-Content -Path '%HASH_PATH%' -Value $line -Encoding ascii; if ((Get-Item '%HASH_PATH%').Length -le 0) { throw 'SHA256 file is empty' }; Write-Host ('SHA256: {0}' -f $line)"

if errorlevel 1 (
  echo.
  echo SHA256 generation failed.
  exit /b 1
)

echo.
echo Packaged to:
echo %ZIP_PATH%
echo SHA256 file:
echo %HASH_PATH%
