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

## Подготовка GitHub Release

Package workflow запускается вручную или на tag `v*` и только подготавливает артефакты. Он не создает и не публикует GitHub Release автоматически. В GitHub Actions UI workflow появится после того, как `.github/workflows/package.yml` будет доступен в default branch репозитория.

Для tag-сборки артефакты получают имя версии/ref, например:

```text
AI-Launcher-Studio-v1.0.0-win-x64
AI-Launcher-Studio-v1.0.0-release-notes
AI-Launcher-Studio-v1.0.0-release-prep
```

В portable artifact лежат zip и `.sha256`; release notes artifact содержит этот файл, а release prep artifact содержит короткий чеклист ручной публикации.

## Что вошло в последние крупные срезы

- `79dc053 feat(studio): process hf download queues`
  - Очередь Hugging Face теперь умеет последовательно скачивать выбранные GGUF-варианты.
  - Элементы очереди показывают русские статусы: ожидает, скачивается, завершено, ошибка.
  - Ошибка одного файла не останавливает всю очередь.
  - Добавлены backend capability-фильтры HF: GGUF, визуальные, инструменты, MTP, runtime compatibility, TurboQuant.
  - Добавлены visual QA docs/script и smoke-тест Package workflow.

- текущий рабочий срез после `79dc053`
  - Capability-фильтры выведены в GUI рядом с HF сортировкой.
  - HF toolbar стал переносимым, чтобы не давить layout на узких окнах.
  - Labels фильтра возможностей русифицированы.

- `059ee51 feat(studio): queue downloads and prepare releases`
  - Добавлены add/remove для очереди HF.
  - Package workflow получил versioned artifact names, release-prep guide и SHA256 verification.
  - Debug-сборка Desktop больше не зависит от необязательного diagnostics package.

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
