@echo off
setlocal
cd /d "%~dp0"

call "%~dp0publish-ai-launcher-studio.bat"
if errorlevel 1 exit /b 1

set "OUT_DIR=%~dp0publish\AI-Launcher-Studio-win-x64"
set "ZIP_PATH=%~dp0publish\AI-Launcher-Studio-win-x64.zip"

if exist "%ZIP_PATH%" del "%ZIP_PATH%"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Compress-Archive -Path '%OUT_DIR%\*' -DestinationPath '%ZIP_PATH%' -Force"

if errorlevel 1 (
  echo.
  echo Package failed.
  exit /b 1
)

echo.
echo Packaged to:
echo %ZIP_PATH%
