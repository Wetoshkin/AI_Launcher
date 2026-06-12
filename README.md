# AI Launcher Studio

[Русский](README_ru.md)

> 🚧 A major UI redesign is in progress — making it clear for beginners and powerful for pros. Progress: `docs/REDESIGN_PROGRESS_RU.md`.

AI Launcher Studio is a Windows desktop GUI for local AI agents, `llama-server`, GGUF models, Hugging Face discovery, runtime checks, and portable local AI workflows.

The product lives in `src/Launcher.Desktop` and is built with Avalonia UI and .NET 8. The previous root launcher app has been removed; this repository now tracks AI Launcher Studio as the main project.

## Highlights

- Run `llama-server` as a server-only OpenAI-compatible endpoint.
- Launch local agent flows through OpenCode, Kilo, Claw, and Aider.
- Choose projects and model folders from the GUI.
- Browse local GGUF models with filters.
- Search Hugging Face models with sorting, filters, downloads, likes, and a download queue.
- Check runtime, GPU, ports, endpoint readiness, and process status.
- Release an occupied port before launch.
- Configure context, KV cache, MTP, TurboQuant, and anti-loop presets.
- Use saved launch presets.
- Build a portable Windows package with zip + SHA256, GitHub Release workflow, and a no-admin installer helper.

## Run From Source

```powershell
dotnet run --project src\Launcher.Desktop\Launcher.Desktop.csproj --no-restore
```

Or run:

```bat
start-ai-launcher-studio.bat
```

## Build And Test

```powershell
dotnet restore .\AI-Launcher-Studio.sln
dotnet build .\AI-Launcher-Studio.sln --no-restore
dotnet test .\AI-Launcher-Studio.sln --no-build
```

## Portable Build

```bat
publish-ai-launcher-studio.bat
package-ai-launcher-studio.bat
```

Output:

```text
publish\AI-Launcher-Studio-win-x64\
publish\AI-Launcher-Studio-win-x64.zip
publish\AI-Launcher-Studio-win-x64.zip.sha256
```

Portable install with a Start Menu shortcut:

```powershell
.\scripts\Install-PortablePackage.ps1 `
  -ZipPath .\publish\AI-Launcher-Studio-win-x64.zip `
  -Destination D:\AI\AI-Launcher-Studio `
  -CreateStartMenuShortcut
```

## Smoke Checks

- `scripts\Invoke-VisualQa.ps1`
- `scripts\Invoke-RuntimeSmoke.ps1`
- `scripts\Invoke-AgentE2eReadiness.ps1`
- `scripts\Invoke-OpenCodeAgentSmoke.ps1`

Russian release and QA docs:

- `docs\INSTALL_PORTABLE_RU.md`
- `docs\GUI_VISUAL_QA_RU.md`
- `docs\RUNTIME_SMOKE_RU.md`
- `docs\AGENT_E2E_READINESS_RU.md`
- `docs\OPENCODE_AGENT_SMOKE_RU.md`
- `docs\RELEASE_NOTES_RU.md`

## Architecture

```text
src\Launcher.Core      scenarios, profiles, guards, review, decoding presets
src\Launcher.Runtimes  llama.cpp runtime, ports, processes, GPU/VRAM, Ollama preflight
src\Launcher.Models    local GGUF catalog and Hugging Face model discovery/downloads
src\Launcher.Agents    command builders and project config for OpenCode/Kilo/Claw/Aider
src\Launcher.Desktop   Avalonia GUI
tests\                 unit and smoke tests
scripts\               install, visual QA, runtime smoke, agent smoke scripts
docs\                  release and QA documentation
```

## Do Not Commit

- GGUF models.
- runtime binaries under `runtimes\`.
- `publish\`.
- `TestResults\`.
- local user settings.

## License

MIT License. See `LICENSE`.
