# Smoke-проверка OpenCode агента

`scripts\Invoke-OpenCodeAgentSmoke.ps1` проверяет полный минимальный путь:

- стартует `llama-server.exe` с выбранной GGUF;
- создаёт временный проект с `opencode.json`;
- запускает `opencode run` через локальный OpenAI-compatible endpoint;
- ждёт короткий ответ и останавливает процессы.

Скрипт не меняет рабочий проект: временная папка создаётся в `%TEMP%` и удаляется в конце.

## Пример

```powershell
.\scripts\Invoke-OpenCodeAgentSmoke.ps1 `
  -RuntimePath "D:\AI\runtimes\turboquant\tqp-v0.1.1\llama-server.exe" `
  -ModelPath "D:\AI\Models\Qwen\Qwen2.5-Coder-0.5B-Instruct-GGUF\qwen2.5-coder-0.5b-instruct-q4_k_m.gguf" `
  -ContextTokens 16384 `
  -Port 18084
```

Успешный результат печатает:

```text
OPENCODE_AGENT_SMOKE_OK
```

## Почему контекст 16384

OpenCode добавляет свой системный и служебный контекст. На `1024` токенах локальный endpoint может пройти `/v1/models` и даже chat-completion smoke, но настоящий `opencode run` упирается в context overflow. Для agent-smoke используйте хотя бы `16384`, а в GUI для реальной работы обычно выбирайте больше, если модель и память позволяют.

## Ограничения

- Это smoke, а не оценка качества модели.
- Маленькая `0.5B` модель подходит, чтобы проверить связку GUI/runtime/OpenCode, но не заменяет нормальную coding-модель.
- Для Kilo/Claw/Aider нужны отдельные smoke-сценарии, потому что у них отличаются CLI и проектные config-файлы.
