# AI Launcher Studio

[English](README.md)

> 🚧 Идёт большой редизайн интерфейса: цель — понятно новичку и мощно для профи. Прогресс: `docs/REDESIGN_PROGRESS_RU.md`.

AI Launcher Studio - Windows GUI для локальных AI-агентов, `llama-server`, GGUF-моделей и runtime-проверок. Приложение помогает выбрать папку проектов, папку моделей, runtime, агентный CLI, контекст/KV/MTP-параметры, проверить порт и запустить локальный endpoint или агентный сценарий.

Проект собран на Avalonia UI и .NET 8. Основной продукт находится в `src\Launcher.Desktop`; старый root launcher-проект удалён из репозитория.

## Что умеет

- Запуск `llama-server` как server-only endpoint для внешних клиентов.
- Запуск агентных сценариев через OpenCode, Kilo, Claw и Aider.
- Выбор папки проектов и папки моделей.
- Локальный каталог GGUF с фильтрацией.
- Поиск моделей на Hugging Face с сортировкой, фильтрами, рейтингами, downloads/likes и очередью скачивания.
- Проверка runtime, GPU, портов и готовности `/v1/models`.
- Освобождение занятого порта перед запуском.
- KV-cache, context и MTP/TurboQuant параметры с русскими подсказками.
- Пресеты быстрого запуска.
- Светлая оранжевая GUI-тема и русскоязычный интерфейс.
- Portable Windows build, zip checksum, GitHub Release workflow и installer helper без прав администратора.

## Быстрый запуск из исходников

```powershell
dotnet run --project src\Launcher.Desktop\Launcher.Desktop.csproj --no-restore
```

Или двойным кликом:

```bat
start-ai-launcher-studio.bat
```

## Сборка и тесты

```powershell
dotnet restore .\AI-Launcher-Studio.sln
dotnet build .\AI-Launcher-Studio.sln --no-restore
dotnet test .\AI-Launcher-Studio.sln --no-build
```

## Первый portable build

Обычная publish-папка:

```bat
publish-ai-launcher-studio.bat
```

Готовый zip + `.sha256`:

```bat
package-ai-launcher-studio.bat
```

Результат:

```text
publish\AI-Launcher-Studio-win-x64\
publish\AI-Launcher-Studio-win-x64.zip
publish\AI-Launcher-Studio-win-x64.zip.sha256
```

Portable-установка с ярлыком в меню Пуск:

```powershell
.\scripts\Install-PortablePackage.ps1 `
  -ZipPath .\publish\AI-Launcher-Studio-win-x64.zip `
  -Destination D:\AI\AI-Launcher-Studio `
  -CreateStartMenuShortcut
```

## Проверки перед релизом

Visual QA:

```powershell
.\scripts\Invoke-VisualQa.ps1 `
  -ExecutablePath .\publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe `
  -CloseAfterCapture
```

Runtime endpoint smoke:

```powershell
.\scripts\Invoke-RuntimeSmoke.ps1 `
  -RuntimePath "D:\AI\runtimes\turboquant\tqp-v0.1.1\llama-server.exe" `
  -ModelPath "D:\AI\Models\Qwen\Qwen2.5-Coder-0.5B-Instruct-GGUF\qwen2.5-coder-0.5b-instruct-q4_k_m.gguf" `
  -Port 18081 `
  -ContextTokens 1024
```

Agent E2E readiness:

```powershell
.\scripts\Invoke-AgentE2eReadiness.ps1 `
  -RuntimePath "D:\AI\runtimes\turboquant\tqp-v0.1.1\llama-server.exe" `
  -ModelsRoot "D:\AI\Models" `
  -RequiredAgent opencode
```

OpenCode smoke:

```powershell
.\scripts\Invoke-OpenCodeAgentSmoke.ps1 `
  -RuntimePath "D:\AI\runtimes\turboquant\tqp-v0.1.1\llama-server.exe" `
  -ModelPath "D:\AI\Models\Qwen\Qwen2.5-Coder-0.5B-Instruct-GGUF\qwen2.5-coder-0.5b-instruct-q4_k_m.gguf" `
  -ContextTokens 16384 `
  -Port 18084
```

Подробные инструкции:

- `docs\INSTALL_PORTABLE_RU.md`
- `docs\GUI_VISUAL_QA_RU.md`
- `docs\RUNTIME_SMOKE_RU.md`
- `docs\AGENT_E2E_READINESS_RU.md`
- `docs\OPENCODE_AGENT_SMOKE_RU.md`
- `docs\RELEASE_NOTES_RU.md`

## Архитектура

```text
src\Launcher.Core      сценарии, профили, guards, review, decoding presets
src\Launcher.Runtimes  llama.cpp runtime, ports, processes, GPU/VRAM, Ollama preflight
src\Launcher.Models    локальный GGUF-каталог и Hugging Face каталог/скачивание
src\Launcher.Agents    command builders и project config для OpenCode/Kilo/Claw/Aider
src\Launcher.Desktop   Avalonia GUI
tests\                 unit/smoke тесты по слоям
scripts\               portable install, visual/runtime/agent smoke checks
docs\                  русские инструкции по проверке и релизу
```

## Не коммитить

- GGUF-модели.
- `runtimes\` с бинарниками.
- `publish\`.
- `TestResults\`.
- локальные настройки пользователя.

## Лицензия

MIT License. См. `LICENSE`.
