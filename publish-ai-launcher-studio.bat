@echo off
setlocal
cd /d "%~dp0"

set "OUT_DIR=%~dp0publish\AI-Launcher-Studio-win-x64"

dotnet publish src\Launcher.Desktop\Launcher.Desktop.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -o "%OUT_DIR%"

if errorlevel 1 (
  echo.
  echo Publish failed.
  exit /b 1
)

echo.
echo Published to:
echo %OUT_DIR%
echo.
echo Run:
echo %OUT_DIR%\Launcher.Desktop.exe
