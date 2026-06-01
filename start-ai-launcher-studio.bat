@echo off
setlocal
cd /d "%~dp0"
dotnet run --project src\Launcher.Desktop\Launcher.Desktop.csproj --no-restore
