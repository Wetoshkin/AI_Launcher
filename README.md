# AI Launcher Studio

[Русский](README_ru.md)

**AI Launcher Studio** is a Windows desktop app that launches AI in a couple of clicks — locally on your PC or online through a provider. Designed to be clear for beginners while giving pros full control.

Built with .NET 8 and Avalonia UI 12.

## Install

Download the installer from [Releases](https://github.com/Wetoshkin/AI_Launcher/releases) — `AI-Launcher-Studio-Setup-*.exe` (no admin rights, no separate .NET needed), or the portable zip.

## Features

- **Local AI** — runs `llama-server` with your GGUF model and chats with it in a built-in chat. Works on CPU and Intel/AMD GPUs (Vulkan), not only NVIDIA.
- **Online AI** — built-in chat to OpenAI, OpenRouter, Anthropic (Claude), or a custom endpoint. Hiddify proxy support for blocked providers.
- **Memory diagram** — visualizes how a model loads across one/two GPUs and system RAM, and warns if it does not fit.
- **Settings conflict engine** — explains in plain language when settings are incompatible (e.g. MTP without a matching model, TurboQuant without the right build, context above native) and how to fix them.
- **Help on every parameter** — a "?" icon with a clear "what / why / what it affects" explanation.
- **Models** — local GGUF catalog and Hugging Face search highlighting dynamic quants (UD-Q4_K_XL, etc.).
- **Runtimes** — recommends a backend for your hardware and really downloads and installs a llama.cpp build from GitHub.
- **Agents** — launch coding agents (OpenCode, Kilo, Claw, Aider) on a local or online model.
- **Anti-looping** — DRY sampler on by default (the best fix for model repetition); multi-GPU via tensor-split.
- **Expert mode** — free-form llama-server arguments for full control; server log console.
- **Appearance** — light and dark themes, Russian and English, in-app update check.

## Run From Source

```powershell
dotnet run --project src\Launcher.Desktop\Launcher.Desktop.csproj
```

## Build And Test

```powershell
dotnet restore .\AI-Launcher-Studio.sln
dotnet build .\AI-Launcher-Studio.sln --no-restore
dotnet test .\AI-Launcher-Studio.sln --no-build
```

## Architecture

```text
src\Launcher.Core      scenarios, profiles, settings, decoding presets, parameter help
src\Launcher.Runtimes  llama.cpp runtime, ports, processes, hardware/VRAM, conflict engine, memory diagram
src\Launcher.Models    local GGUF catalog and Hugging Face search/downloads
src\Launcher.Agents    coding agent launch and discovery
src\Launcher.Online    online providers and streaming chat (OpenAI-compatible and Anthropic)
src\Launcher.Desktop   Avalonia UI (Home/Chat/Models/Agents/Runtimes/Settings pages)
tests\                 unit tests per layer
```

## License

MIT. See `LICENSE`.
