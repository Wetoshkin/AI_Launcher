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
- Runtime downloader/update manager начат и подключён к GUI:
  - GitHub releases/assets читаются через REST API;
  - stable zip assets фильтруются по имени и runtime-профилю;
  - доступны профили `CPU`, `CUDA`, `Vulkan`, `ROCm`;
  - выбранный runtime можно скачать в безопасный cache path;
  - `.download` temp-файлы чистятся при отмене;
  - уже скачанные архивы пропускаются по размеру;
  - в GUI есть `Найти runtime`, `Скачать выбранный`, `Скачать и установить`;
  - в GUI есть прогресс runtime download и отмена активного скачивания;
  - папки установки runtime и кэша выбираются через folder picker;
  - runtime root/cache сохраняются и загружаются из settings.
- В launch review добавлена оценка памяти:
  - веса GGUF;
  - KV-cache по выбранному контексту/runtime;
  - runtime overhead;
  - сравнение с последним проверенным свободным VRAM.
- Добавлен GUI-контроль `MTP draft` для `--spec-draft-n-max`:
  - диапазон 1..16;
  - значение подставляется в команду `llama-server`;
  - подсказка объясняет компромисс скорость/стабильность.
- Добавлены GUI-контролы KV cache:
  - отдельный выбор `--cache-type-k` и `--cache-type-v`;
  - TurboQuant default: `q8_0/turbo4`;
  - обычный llama.cpp default: `q8_0/q8_0`;
  - forecast памяти использует выбранные K/V типы.
- Нижняя часть GUI разбита на вкладки:
  - `Runtime`;
  - `Агенты`;
  - `Модели` с локальным каталогом и Hugging Face поиском.
- Добавлена публикация Windows portable build:
  - `publish-ai-launcher-studio.bat`;
  - `package-ai-launcher-studio.bat`;
  - локальный zip: `publish\AI-Launcher-Studio-win-x64.zip`;
  - проверен старт опубликованного `Launcher.Desktop.exe`.
- Добавлен запуск server перед agent CLI:
  - для agent-сценария поднимается `llama-server`;
  - после готовности endpoint пишется `kilo.jsonc` или `opencode.json`;
  - затем запускается выбранный CLI агента.
- Добавлен live output процессов в GUI:
  - stdout/stderr `llama-server` и CLI агента идут в лог-панель;
  - активные PID можно остановить кнопкой `Остановить`.
- Установленный runtime сразу активируется в GUI:
  - команда запуска использует найденный `llama-server.exe`;
  - отдельный повторный scan больше не нужен для базового запуска.
- Agent/server model id синхронизирован:
  - `llama-server` получает `--alias local/<имя GGUF>`;
  - agent CLI и проектные конфиги используют тот же provider model id;
  - preview agent-сценария показывает обе стадии: `SERVER` и `AGENT`.
- Добавлен GitHub Actions CI:
  - restore/build/test решения;
  - publish Windows portable artifact;
  - проверка наличия опубликованного exe.

## Что ещё надо сделать

- Доработать runtime downloader/update manager:
  - добавить явный выбор источника/канала runtime;
  - добавить update-check для уже установленного runtime.
- Вынести большой `HomeViewModel` на отдельные VM/экраны: Dashboard, Launch, Models, Runtimes, Agents, Logs, Settings.
- Довести tabs/navigation до отдельных view model и отдельных XAML views.
- Добавить остальные speculative decoding параметры как полноценные controls.
- Улучшить VRAM forecast до per-GPU панели и учитывать K/V cache типы из GUI, когда они появятся.
- Добавить end-to-end smoke tests запуска:
  - endpoint-only llama-server;
  - agent + local endpoint;
  - busy port release;
  - missing CLI/runtime/model.
- Добавить packaging/release:
  - installer;
  - release workflow/tag publishing;
  - подпись/хэши артефактов.
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
- `bcf4370 feat(runtimes): read GitHub release assets`
- `c33a7af feat(runtimes): download release assets to cache`
- `d671abd feat(desktop): download runtime releases from gui`
- `4ed6be8 feat(desktop): download and install runtime release`
- `fb2d278 feat(desktop): choose runtime folders`
- `186eade feat(desktop): persist runtime folders`
- `826fc9e feat(runtimes): filter release assets by profile`
- `2e1ad17 feat(runtimes): report runtime download progress`
- `6cb39fa feat(desktop): cancel runtime downloads`
- `dbe2362 style(desktop): rebuild studio home screen`
- `0599164 build(desktop): add studio publish script`
- `4d7e040 feat(desktop): release occupied llama server port`
- `adbf6cd feat(desktop): stop active launch process`
- `0a1cd96 feat(desktop): stream process output to launch log`
- `a0d36ed feat(desktop): start server before agent cli`
- `43a16d1 feat(agents): write local project configs`
- `c7354d7 fix(desktop): activate installed runtime`
- `71ea0f5 fix(agents): align local model ids`
- `2f366b9 feat(desktop): show full agent launch preview`
- `b7c4c9e ci: add desktop build workflow`
- `104f331 feat(desktop): control kv cache types`

## Рекомендованный следующий срез для второго агента

1. Довести Launch UI до production-качества:
   - вынести текущие вкладки в отдельные views/view models;
   - добавить полноценные controls для speculative decoding;
   - добавить очистку лога и сохранение последнего launch log.
2. Добавить end-to-end smoke tests:
   - endpoint-only с тестовым `llama-server` stub;
   - agent + local endpoint;
   - occupied port release;
   - missing CLI/runtime/model.
3. Улучшить runtime downloader:
   - channel/source в GUI;
   - update-check для уже установленного runtime;
   - подписи/подсказки к CPU/CUDA/Vulkan/ROCm профилям.
4. Запустить полный `dotnet build` + `dotnet test`, затем проверить CI в GitHub Actions.
