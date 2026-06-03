# AI Launcher Studio: handoff для второго AI-агента

## Где лежит работа

- Рабочий каталог: ветка `main-ai-launcher-studio-full-port` / GitHub branch `ai-launcher-studio-full-port`
- Текущая ветка: `main-ai-launcher-studio-full-port`
- Целевая публикация: `Wetoshkin/AI_Launcher`, ветка `ai-launcher-studio-full-port`
- Целевой репозиторий продукта: `Wetoshkin/AI_Launcher`

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
- Добавлен Hugging Face quant-фильтр по GGUF-файлам:
  - `любой`;
  - `Q4_K_M`;
  - `Q5_K_M`;
  - `Q6_K`;
  - `Q8_0`.
- Добавлен Hugging Face family-фильтр по repo id, tags и именам GGUF-файлов:
  - `любая`;
  - `Qwen`;
  - `DeepSeek`;
  - `Gemma`;
  - `Llama`;
  - `Mistral`.
- Добавлены Hugging Face capability-фильтры:
  - `все возможности`;
  - `GGUF`;
  - `визуальные`;
  - `инструменты`;
  - `MTP`;
  - `совместимые runtime`;
  - `TurboQuant`.
- Hugging Face model metadata теперь сохраняет размер GGUF-файлов, если API отдаёт `size`, `sizeBytes` или `lfs.size`:
  - размер переносится в варианты скачивания;
  - split-shards суммируются в общий размер;
  - если размера нет, UI/backend не падают и показывают пустую строку.
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
- Добавлена очередь HF-скачиваний:
  - добавление и удаление выбранного GGUF-варианта;
  - дедупликация по repo/file list;
  - последовательное скачивание;
  - статусы `ожидает скачивания`, `скачивается`, `завершено`, `ошибка`;
  - ошибка одного элемента не останавливает всю очередь.
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
- Agent command builders для локального OpenAI-compatible endpoint теперь отклоняют model id без `local/`, чтобы preview/start не уходил в чужой provider model.
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
  - update-check работает не только от выбранного zip-архива, но и от обнаруженного `llama-server.exe`, если путь содержит build tag вроде `b5300`;
  - источник версии runtime сохраняется в settings как `lastRuntimeVersionSource` и переживает перезапуск.
- Runtime release packages имеют source/channel metadata:
  - `stable`;
  - `latest`;
  - `manual`;
  - `detected`;
  - человекочитаемые подписи возвращаются на русском языке.
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
- Settings/profile persistence расширены:
  - профили сохраняют KV cache параметры;
  - профили сохраняют MTP/speculative параметры;
  - settings сохраняют Hugging Face фильтры.
- Busy port safety усилен:
  - неизвестный процесс на порту не освобождается автоматически;
  - старт процесса блокируется;
  - GUI получает русскую строку статуса вроде `порт 8080: занят postgres`.
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
- Добавлен GitHub Actions Package workflow:
  - ручной запуск через `workflow_dispatch`;
  - запуск по тегам `v*`;
  - build/test/publish;
  - portable zip artifact вместе с `.zip.sha256`;
  - SHA256 выводится в лог package workflow и локального package bat;
  - проверка, что в zip не попали GGUF и временные `.download` файлы.

## Что ещё надо сделать

- Доработать runtime downloader/update manager:
  - добавить явный выбор источника/канала runtime;
  - привязать новые source/channel labels к GUI-контролам.
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
  - подпись артефактов.
- Добавить браузерную/визуальную проверку GUI скриншотами после крупных UI-изменений.
- Улучшить Hugging Face UX:
  - отдельные фильтры size/MTP/vision/tools;
  - вывести уже собранные `FormattedSize`/`TotalSizeBytes` в GUI;
  - очередь загрузок.
- Не коммитить `runtimes/`, модели, скачанные GGUF, временные `.download`.

## Что запустить прямо сейчас

Минимальная проверка свежего handoff и portable-сборки:

```powershell
dotnet build .\AI-Launcher-Studio.sln --no-restore
dotnet test .\AI-Launcher-Studio.sln --no-build
.\package-ai-launcher-studio.bat
```

Если нужно только открыть текущий GUI из исходников:

```powershell
.\start-ai-launcher-studio.bat
```

или:

```powershell
dotnet run --project src\Launcher.Desktop\Launcher.Desktop.csproj --no-restore
```

Для проверки опубликованной portable-сборки после упаковки:

```powershell
.\publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe
```

Готовый zip-архив лежит здесь:

```text
publish\AI-Launcher-Studio-win-x64.zip
```

На базе `e5b1617` архив уже был собран локально: `publish\AI-Launcher-Studio-win-x64.zip`.

## Как собрать portable zip

1. Убедиться, что рабочее дерево не содержит чужих незавершённых изменений в файлах, которые нужны для упаковки.
2. Запустить:

```powershell
.\package-ai-launcher-studio.bat
```

3. Проверить, что появились:
   - папка `publish\AI-Launcher-Studio-win-x64`;
   - архив `publish\AI-Launcher-Studio-win-x64.zip`.
4. Открыть опубликованный exe:

```powershell
.\publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe
```

5. Проверить, что в zip не попали модели, runtime binaries из `runtimes\`, временные `.download` файлы и локальные конфиги пользователя.

## Как проверять GUI

Быстрый smoke-check без реального запуска модели:

- Открыть GUI из исходников или из portable-папки.
- Проверить, что первый экран сразу показывает рабочий dashboard AI Launcher Studio, а не landing page.
- Перейти по вкладкам `Runtime`, `Агенты`, `Модели`.
- В `Runtime` проверить выбор папок runtime root/cache и кнопку поиска runtime.
- В `Агенты` проверить отображение CLI discovery для `opencode`, `kilo`, `claw`, `aider`, `pi`.
- В `Модели` проверить локальный каталог GGUF и Hugging Face search; для HF проверить quant/family filters.
- В launch preview проверить, что agent-сценарий показывает две стадии: `SERVER` и `AGENT`.
- Ввести занятый порт и убедиться, что GUI показывает русское предупреждение о занятом порте, не пытаясь освобождать неизвестный процесс автоматически.
- Проверить кнопку `Остановить` и очистку live log, если был запущен stub/runtime.

Расширенный GUI smoke-check с реальным runtime:

- Выбрать или скачать совместимый `llama-server.exe`.
- Выбрать небольшой локальный GGUF.
- Запустить server-only endpoint и дождаться успешного `GET /v1/models`.
- Запустить agent-сценарий с установленным CLI и проверить, что проектный config получает model id вида `local/<имя GGUF>`.
- После остановки убедиться, что порт освобождён, а лог не содержит необъяснённых исключений.

## Независимые задачи следующей волны

Эти задачи можно раздать разным агентам, если заранее развести файлы владения:

- Runtime UX: source/channel selector, подписи профилей `CPU`/`CUDA`/`Vulkan`/`ROCm`, отображение установленной и сохранённой версии runtime.
- Launch UX: отдельные controls для остальных speculative decoding параметров и улучшение launch review.
- Models UX: фильтры size/MTP/vision/tools, вывод `FormattedSize`/`TotalSizeBytes`, очередь загрузок HF GGUF.
- Tests: end-to-end smoke tests со stub `llama-server`, agent + local endpoint, occupied port, missing CLI/runtime/model.
- Packaging/release: installer, checksums, подпись артефактов, release notes.
- Architecture: разбиение `HomeViewModel` на отдельные VM/экраны и перенос вкладок в отдельные XAML views.
- Visual QA: сценарии браузерной/скриншотной проверки GUI после крупных UI-изменений.

## Файлы, которые нельзя трогать одновременно

При параллельной работе не давайте двум агентам одновременно править одни и те же зоны:

- `src\Launcher.Desktop\ViewModels\HomeViewModel.cs`: центральная точка GUI-состояния, runtime/model/agent launch логика.
- `src\Launcher.Desktop\Views\HomeView.axaml`: основной layout и вкладки.
- `src\Launcher.Core\*`: launch plan, settings, compatibility review, presets.
- `src\Launcher.Runtimes\*`: runtime discovery/download/install, process/port/endpoint logic.
- `src\Launcher.Models\*`: локальный каталог, Hugging Face search/download, file grouping.
- `src\Launcher.Agents\*`: command builders, CLI discovery, project config writers.
- `tests\Launcher.*.Tests\*`: тесты рядом с соответствующей доменной зоной.
- `publish-ai-launcher-studio.bat`, `package-ai-launcher-studio.bat`, `.github\workflows\*.yml`: packaging/CI ownership.
- `README.md`, `README_ru.md`, `docs\AI_LAUNCHER_STUDIO_HANDOFF_RU.md`: документация и handoff; менять синхронно только одним агентом.

Если два среза всё же затрагивают одну зону, сначала договориться о порядке: один агент заканчивает и оставляет diff, второй перечитывает свежий файл и продолжает поверх него.

## Методика разработки

- Работать маленькими вертикальными срезами.
- На каждую новую функцию писать failing test до production-кода.
- Цикл: RED -> GREEN -> refactor -> targeted tests -> full build/test -> commit.
- Для C#-части после среза запускать:

```powershell
dotnet build .\AI-Launcher-Studio.sln --no-restore
dotnet test .\AI-Launcher-Studio.sln --no-build
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
- `b16b5d3 style(desktop): organize studio sections into tabs`
- `c65b568 feat(desktop): clear process log`
- `f01d061 build(desktop): add portable zip package script`
- `2b211e2 fix(desktop): check updates for detected runtime`
- `de2221a feat(desktop): persist runtime version source`
- `c0bddcf feat(desktop): show runtime version source`
- `eca06c8 feat(models): filter hugging face results by quant`
- `e5feb32 feat(models): filter hugging face results by family`
- `82fb753 feat(studio): harden metadata launch and packaging`
- `059ee51 feat(studio): queue downloads and prepare releases`
- `79dc053 feat(studio): process hf download queues`

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
   - явно отображать установленную/сохранённую версию runtime;
   - подписи/подсказки к CPU/CUDA/Vulkan/ROCm профилям.
4. Запустить полный `dotnet build` + `dotnet test`, затем проверить CI в GitHub Actions.
5. Довести визуальную QA-проверку:
   - снять screenshot текущего окна после каждого крупного XAML-среза;
   - проверить нижние блоки на высоте около `720px`;
   - если блоки не видны без scroll, убедиться, что scroll работает ожидаемо и это не выглядит как обрезанный UI.
