<div align="center">

<img src="docs/images/app-icon.png" width="120" alt="AI Launcher Studio"/>

# AI Launcher Studio

**Launch AI in a couple of clicks — locally on your PC or online.**
Clear for beginners, powerful for pros.

[![Release](https://img.shields.io/github/v/release/Wetoshkin/AI_Launcher?label=release)](https://github.com/Wetoshkin/AI_Launcher/releases)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
![Platform](https://img.shields.io/badge/Windows-10%2F11-0078D6)
![.NET](https://img.shields.io/badge/.NET-8-512BD4)

[Русская версия](README.md)

<img src="docs/images/screenshot-home.png" width="85%" alt="Home"/>

</div>

---

## ⚡ What is it

**AI Launcher Studio** is a Windows desktop app that removes all the pain of running local and online AI. No terminal, no Python, no flag wrangling. Press a couple of buttons and chat with a model — on your own hardware (free and private) or via a cloud provider.

Under the hood it has all the advanced capabilities: real `llama.cpp` engine install, a visual memory diagram, a settings conflict engine, anti-looping, multi-GPU, MoE, reasoning models, and an Expert mode with full control.

## 📦 Install

Download from **[Releases](https://github.com/Wetoshkin/AI_Launcher/releases)**:

- **`AI-Launcher-Studio-Setup-*.exe`** — installer (no admin rights, shortcuts, uninstaller). Just run it.
- **`AI-Launcher-Studio-portable-*.zip`** — portable build (unzip and run `Launcher.Desktop.exe`).

No separate .NET install needed — everything is bundled.

## ✨ Features

### 💻 Local AI on your PC
Install a `llama.cpp` engine for your hardware (CPU / Vulkan / CUDA / ROCm — the app recommends one), pick a GGUF model and launch. The built-in chat streams replies in real time. Works on Intel/AMD GPUs, not only NVIDIA.

### ☁️ Online AI
Built-in chat to **OpenAI**, **OpenRouter**, **Anthropic (Claude)** or a custom endpoint via API key. Hiddify proxy support for blocked providers.

<div align="center"><img src="docs/images/screenshot-chat.png" width="85%" alt="Chat"/></div>

### 🧠 Memory diagram
Visualizes **how a model fits** across one or two GPUs and system RAM, and warns if it doesn't — before you launch.

### ⚠️ Settings conflict engine + hints
Explains in plain language when settings are incompatible (MTP without a matching model, TurboQuant without the right build, context above native, out of memory) and how to fix them. **Every** parameter has a "?" icon explaining what it is, why, and what it affects.

<div align="center"><img src="docs/images/screenshot-settings.png" width="85%" alt="Settings"/></div>

### 📦 Models & 🤖 Runtimes
Local GGUF catalog and live **Hugging Face** search highlighting dynamic quants (UD-Q4_K_XL). Real **download and install** of `llama.cpp` builds from inside the app.

<div align="center"><img src="docs/images/screenshot-runtimes.png" width="85%" alt="Runtimes"/></div>

### 🤖 Coding agents
Launch agents (**OpenCode, Kilo, Claw, Aider**) on a local or online model right in your project.

## 🚀 Highlights

- **🚫 Anti-looping** — DRY sampler on by default (the best fix for repetition; naive repeat-penalty can make loops worse).
- **🎮 Multi-GPU** — `tensor-split` proportional to VRAM for setups like RTX 3090 + RTX 3060.
- **🧩 MoE on CPU** — auto-detects Mixture-of-Experts models (Qwen3 MoE, Mixtral, GLM) and offloads experts to RAM via a slider so big MoE fits in VRAM. Auto-calculated + manual.
- **💭 Reasoning models** — thinking mode with a budget; the `<think>` block collapses into its own section, keeping the answer clean.
- **🎨 Response style** — Precise (code) / Balanced / Creative in one click.
- **🌗 Light and dark themes**, **🌍 Russian and English**, **🔄 update check**.
- **🛠 Expert mode** — free-form `llama-server` arguments + server log console.

<div align="center"><img src="docs/images/screenshot-home-dark.png" width="85%" alt="Dark theme"/></div>

## 🏁 Quick start

1. **Runtimes** → "Find builds" → "Download and install" the recommended one (Vulkan for Intel/AMD, CUDA for NVIDIA).
2. **Models** → find a model on Hugging Face or point to a folder of `.gguf` files.
3. **Chat** → "Local server" → pick a model → "Start". When the status says ready, type messages.

Or just **Chat** → pick an online provider, enter an API key, and chat.

## 🧱 Architecture

```text
src/Launcher.Core      scenarios, profiles, settings, decoding presets, parameter help
src/Launcher.Runtimes  llama.cpp runtime, ports, processes, hardware/VRAM, conflict engine, memory diagram
src/Launcher.Models    local GGUF catalog and Hugging Face search/downloads
src/Launcher.Agents    coding agent launch and discovery
src/Launcher.Online    online providers and streaming chat (OpenAI-compatible and Anthropic)
src/Launcher.Desktop   Avalonia UI (Home / Chat / Models / Agents / Runtimes / Settings)
tests/                 200+ unit tests per layer
```

Tech: **.NET 8**, **Avalonia UI 12**, **Manrope** font.

## 🔧 Build from source

```powershell
dotnet restore .\AI-Launcher-Studio.sln
dotnet build   .\AI-Launcher-Studio.sln --no-restore
dotnet test    .\AI-Launcher-Studio.sln --no-build
dotnet run --project src\Launcher.Desktop\Launcher.Desktop.csproj
```

## 📄 License

[MIT](LICENSE) © Wetoshkin
