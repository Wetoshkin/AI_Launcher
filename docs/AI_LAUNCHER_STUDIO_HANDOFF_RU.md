# AI Launcher Studio: handoff для второго AI-агента

## Где лежит работа

- Рабочий каталог: `D:\AI\LlamaServerLauncherAvalonia\.worktrees\ai-launcher-studio-full-port`
- Текущая ветка: `main-ai-launcher-studio-full-port`
- Целевая публикация: `Wetoshkin/AI_Launcher`, ветка `ai-launcher-studio-full-port`
- Исходная база: форк `Wetoshkin/LlamaServerLauncherAvalonia`

## Что уже сделано

- Создана модульная .NET/Avalonia-архитектура:
  - `Launcher.Core`: сценарии, профили, настройки, launch plan, guard/review, anti-loop presets.
  - `Launcher.Models`: локальный GGUF-каталог, фильтры, Hugging Face search, выбор GGUF-файлов, скачивание.
  - `Launcher.Runtimes`: GPU/VRAM, порты, процессы, llama.cpp capabilities, runtime install, endpoint health.
  - `Launcher.Agents`: command builders и discovery для OpenCode/Kilo/Claw/Aider/Pi.
  - `Launcher.Desktop`: новый Avalonia GUI shell.
- GUI переведён на русский, светло-оранжевая тема, рабочий dashboard вместо landing page.
- Добавлены два режима запуска: проектный агент и server-only endpoint.
- Добавлен выбор папки моделей и проектов.
- Добавлен локальный каталог GGUF с поиском и выбором конкретной модели.
- Добавлен Hugging Face GGUF search с сортировкой, downloads/likes/tags, фильтром `gguf`.
- Добавлен выбор конкретных `.gguf` файлов внутри HF repo:
  - фильтруются `mmproj` и не-GGUF файлы;
  - split-shards группируются в один вариант скачивания;
  - строятся безопасные `resolve/main` URLs.
- Добавлено скачивание HF GGUF:
  - single и split-файлы;
  - прогресс по байтам;
  - отмена;
  - пропуск уже скачанных файлов;
  - защита от path traversal.
- Добавлены реальные пресеты быстрого запуска:
  - применяются к агенту/runtime/project/context/port;
  - сохраняются в JSON settings;
  - загружаются при старте.
- Добавлены редактируемые `Port` и `ContextTokens`, вместо жёсткого `8080` и `65536`.
- Добавлена проверка агентных CLI в PATH:
  - `opencode`, `kilo`, `claw`, `aider`, `pi`;
  - отображение version/path/status в GUI.
- Добавлен запуск runtime/агента через launch plans:
  - OpenCode/Kilo/Claw/Aider command builders;
  - process starter;
  - port guard/release для безопасного освобождения `llama-server`.
- Добавлена готовность endpoint после старта:
  - polling `GET /v1/models`;
  - запуск не считается успешным только по PID.
- Добавлена установка runtime из zip:
  - распаковка в выбранный runtime root;
  - поиск `llama-server.exe`;
  - защита от zip-slip.
- Добавлена доменная проверка совместимости runtime:
  - MTP требует `--spec-type draft-mtp`;
  - TurboQuant требует TurboQuant capability;
  - обычный llama.cpp/Ollama проходят отдельно.
- Проверка совместимости runtime подключена в GUI:
  - warning виден в preview/review;
  - несовместимый runtime блокирует старт до запуска процесса.
- Добавлен file picker для zip-архива runtime, рядом с ручным вводом пути.
- В launch review добавлена оценка памяти:
  - веса GGUF;
  - KV-cache по выбранному контексту/runtime;
  - runtime overhead;
  - сравнение с последним проверенным свободным VRAM.
- Добавлен GUI-контроль `MTP draft` для `--spec-draft-n-max`:
  - диапазон 1..16;
  - значение подставляется в команду `llama-server`;
  - подсказка объясняет компромисс скорость/стабильность.

## Что ещё надо сделать

- Сделать полноценный runtime downloader/update manager из GitHub releases/официальных источников, а не только установку zip.
- Вынести большой `HomeViewModel` на отдельные VM/экраны: Dashboard, Launch, Models, Runtimes, Agents, Logs, Settings.
- Сделать реальные tabs/navigation вместо текущего длинного dashboard.
- Добавить подробный лог процессов и live output `llama-server`/CLI агента в GUI.
- Добавить настройку KV cache и остальные speculative decoding параметры как полноценные controls.
- Улучшить VRAM forecast до per-GPU панели и учитывать K/V cache типы из GUI, когда они появятся.
- Добавить end-to-end smoke tests запуска:
  - endpoint-only llama-server;
  - agent + local endpoint;
  - busy port release;
  - missing CLI/runtime/model.
- Добавить packaging/release:
  - `dotnet publish`;
  - portable Windows artifact;
  - installer или zip build;
  - GitHub Actions CI.
- Добавить браузерную/визуальную проверку GUI скриншотами после крупных UI-изменений.
- Улучшить Hugging Face UX:
  - отдельные фильтры family/quant/size/MTP/vision/tools;
  - отображение размера файлов, если API/metadata позволяют;
  - очередь загрузок.
- Не коммитить `runtimes/`, модели, скачанные GGUF, временные `.download`.

## Методика разработки

- Работать маленькими вертикальными срезами.
- На каждую новую функцию писать failing test до production-кода.
- Цикл: RED -> GREEN -> refactor -> targeted tests -> full build/test -> commit.
- Для C#-части после среза запускать:

```powershell
dotnet build .\llama-server-launcher-avalonia.sln --no-restore
dotnet test .\llama-server-launcher-avalonia.sln --no-build
```

- Для точечных проверок использовать project-level tests:

```powershell
dotnet test tests\Launcher.Core.Tests\Launcher.Core.Tests.csproj --filter <TestClass>
dotnet test tests\Launcher.Models.Tests\Launcher.Models.Tests.csproj --filter <TestClass>
dotnet test tests\Launcher.Runtimes.Tests\Launcher.Runtimes.Tests.csproj --filter <TestClass>
dotnet test tests\Launcher.Agents.Tests\Launcher.Agents.Tests.csproj --filter <TestClass>
dotnet test tests\Launcher.Desktop.Tests\Launcher.Desktop.Tests.csproj --filter <TestClass>
```

- Перед утверждением "готово" обязательно иметь свежий вывод build/test.
- Коммиты делать небольшими, с понятным scope:
  - `feat(models): ...`
  - `feat(runtimes): ...`
  - `feat(desktop): ...`
  - `docs: ...`
- Не делать destructives:
  - не использовать `git reset --hard`;
  - не откатывать чужие изменения;
  - не удалять runtime/model artifacts за пределами явно выбранной рабочей папки.
- Для файловых правок использовать `apply_patch`.
- Для поиска использовать `rg` / `rg --files`.

## Последние важные коммиты

- `51e525b feat(runtimes): validate runtime compatibility`
- `8900ef9 feat(runtimes): install runtime packages from zip`
- `d5439ff feat(runtimes): wait for endpoint readiness`
- `4d58fcb feat(agents): detect installed agent CLIs`
- `f0d57e5 feat(desktop): edit launch port and context`
- `6f7b582 feat(desktop): persist launch presets`
- `167dcee feat(desktop): apply saved launch presets`
- `7b2f00e feat(models): show download progress and cancellation`
- `722de75 feat(models): download Hugging Face GGUF files`
- `d0f8c24 feat(models): expose Hugging Face GGUF downloads`
- `cc85f02 feat(desktop): enforce runtime compatibility`
- `2d1bf4d feat(desktop): choose runtime archive file`
- `0ac065f feat(desktop): show launch memory forecast`
- `a405a0e feat(desktop): control mtp draft tokens`

## Рекомендованный следующий срез для второго агента

1. Сделать runtime downloader/update manager:
   - получить список релизов llama.cpp/TurboQuant runtime;
   - показать версии в GUI;
   - скачать zip в downloads/cache;
   - переиспользовать текущий `RuntimePackageInstaller`.
2. Покрыть `Launcher.Runtimes.Tests` и `Launcher.Desktop.Tests`:
   - релиз с подходящим asset выбирается;
   - zip скачивается в безопасный путь;
   - GUI показывает ошибку сети и не затирает установленный runtime;
   - установка после скачивания обновляет `RuntimeStatus`.
3. Запустить полный `dotnet build` + `dotnet test`.
