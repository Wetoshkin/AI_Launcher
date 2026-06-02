# AI Launcher Studio: заметки текущей сборки

## Что уже можно запускать

- GUI из исходников:

```powershell
.\start-ai-launcher-studio.bat
```

- Portable-сборка:

```powershell
.\package-ai-launcher-studio.bat
.\publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe
```

- Архив и checksum:

```text
publish\AI-Launcher-Studio-win-x64.zip
publish\AI-Launcher-Studio-win-x64.zip.sha256
```

## Что вошло в последние крупные срезы

- `82fb753 feat(studio): harden metadata launch and packaging`
  - HF GGUF metadata хранит размеры файлов и split-групп.
  - Agent command builders требуют `local/<gguf>` для локального OpenAI-compatible endpoint.
  - Settings/profile persistence сохраняет KV cache, MTP и Hugging Face filters.
  - Runtime release packages получили source/channel metadata с русскими labels.
  - Busy port guard не освобождает неизвестные процессы автоматически.
  - Добавлен Package workflow для portable zip.

- `0765cc4 feat(studio): expose runtime sources and package hashes`
  - Runtime UI показывает и фильтрует source/channel: стабильный релиз, последний релиз, ручной выбор, автообнаружение.
  - Hugging Face UI показывает размер GGUF-вариантов, если API отдаёт metadata.
  - Desktop smoke tests покрывают fake endpoint launch и missing model guard.
  - Package workflow и локальный bat создают `.zip.sha256`.
  - README и handoff обновлены под portable-сборку и параллельную разработку.

## Как продолжать второму агенту

1. Перед началом проверить:

```powershell
git status --short
dotnet build .\llama-server-launcher-avalonia.sln --no-restore
dotnet test .\llama-server-launcher-avalonia.sln --no-build
```

2. Брать маленький независимый срез и заранее объявлять ownership файлов.
3. Для behavior changes использовать TDD: RED -> GREEN -> refactor.
4. После среза запускать targeted tests, затем full build/test.
5. Не коммитить `publish\`, `runtimes\`, модели, `.download` и локальные конфиги.

## Пять задач до условного 1.0

- Разбить `HomeViewModel` и `HomeView.axaml` на отдельные экраны: Launch, Models, Runtimes, Agents, Logs, Settings.
- Довести HF UX: фильтры size/MTP/vision/tools и очередь загрузок.
- Добавить полноценные controls speculative decoding помимо MTP draft tokens.
- Сделать визуальную QA-проверку GUI скриншотами на desktop/mobile-like размерах окна.
- Дособрать release pipeline: installer, подпись артефактов, release notes в GitHub Release.
