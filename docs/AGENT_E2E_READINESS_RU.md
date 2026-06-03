# Readiness-проверка перед настоящим agent E2E

`scripts\Invoke-AgentE2eReadiness.ps1` проверяет, есть ли на машине минимальные предпосылки для настоящего agent E2E:

- runtime `llama-server*.exe`;
- хотя бы одна вероятная чат/кодинг GGUF-модель;
- нужные CLI агенты в `PATH`: `opencode`, `kilo`, `claw`, `aider`.

Скрипт read-only: он не скачивает модели, не запускает генерацию, не стартует runtime и не меняет файлы. Его задача - явно показать блокеры до запуска E2E, чтобы не путать проблемы окружения с ошибками приложения.

## Примеры

Проверить все агенты и автоматически поискать runtime/model roots в очевидных местах:

```powershell
.\scripts\Invoke-AgentE2eReadiness.ps1
```

Проверить конкретный runtime, папку моделей и только OpenCode:

```powershell
.\scripts\Invoke-AgentE2eReadiness.ps1 `
  -RuntimePath "D:\AI\runtimes\turboquant\tqp-v0.1.1\llama-server.exe" `
  -ModelsRoot "D:\AI\Models" `
  -RequiredAgent opencode
```

Снизить минимальный размер модели, если используется очень маленькая чат-модель:

```powershell
.\scripts\Invoke-AgentE2eReadiness.ps1 -ModelsRoot "D:\AI\Models" -MinModelSizeGb 0.1
```

## Что считается успехом

Успешный результат печатает:

```text
AGENT_E2E_READY
```

Это значит, что найден runtime, найдена хотя бы одна вероятная чат/кодинг GGUF и найден каждый требуемый CLI агент.

Если чего-то не хватает, скрипт печатает:

```text
AGENT_E2E_BLOCKED
```

и список блокеров. Exit code в этом случае равен `1`.

## Важное про embedding GGUF

Embedding-модели по имени или пути вроде `nomic-embed`, `bge`, `e5`, `embedding` или `embed` помечаются как `NOT_AGENT_MODEL`.

Такая GGUF может подходить для runtime smoke-проверки `llama-server.exe` и endpoint `/v1/models`, но не является чат/кодинг моделью для настоящего agent E2E. Для агентного сценария нужна модель, которая умеет отвечать в диалоге и выполнять coding-agent workflow через выбранный CLI.

## Параметры

- `-RuntimePath` - явный путь к `llama-server.exe`; если не задан, скрипт ищет `llama-server*.exe` в очевидных runtime-папках.
- `-ModelsRoot` - явная папка с GGUF; если не задана, скрипт смотрит типичные локальные папки моделей.
- `-ProjectRoot` - корень проекта; по умолчанию вычисляется от папки `scripts`.
- `-RequiredAgent` - `opencode`, `kilo`, `claw`, `aider` или `all`; по умолчанию `all`.
- `-MinModelSizeGb` - минимальный размер вероятной чат/кодинг GGUF; по умолчанию `0.2`.
