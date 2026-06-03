# Реальная smoke-проверка runtime

Этот сценарий проверяет, что конкретный `llama-server.exe` действительно стартует с выбранной GGUF-моделью и отвечает на `/v1/models`.

Он не заменяет полноценный агентный E2E: для агентного сценария нужна чат/кодинг-модель и установленный CLI агент. Но smoke быстро ловит главные проблемы runtime: битый exe, неподходящая модель, занятый порт, ошибки загрузки модели.

## Пример

```powershell
.\scripts\Invoke-RuntimeSmoke.ps1 `
  -RuntimePath "D:\AI\runtimes\turboquant\tqp-v0.1.1\llama-server.exe" `
  -ModelPath "C:\Users\Wetoshkin\.lmstudio\.internal\bundled-models\nomic-ai\nomic-embed-text-v1.5-GGUF\nomic-embed-text-v1.5.Q4_K_M.gguf" `
  -Port 18080 `
  -ContextTokens 512 `
  -GpuLayers 0 `
  -Embeddings
```

Для обычной чат/кодинг GGUF-модели `-Embeddings` обычно не нужен.

## Что считается успехом

Скрипт печатает:

```text
REAL_RUNTIME_SMOKE_OK
```

а также PID, endpoint и JSON-ответ `/v1/models`.

После проверки процесс `llama-server` останавливается автоматически. Если runtime не поднялся, скрипт печатает хвост stdout/stderr и выходит с ошибкой.

## Замечания

- По умолчанию используется порт `18080`, чтобы не конфликтовать с обычным `8080`.
- Если выбранный порт уже занят, скрипт остановится до запуска runtime и попросит выбрать другой `-Port`.
- `-GpuLayers 0` запускает CPU-only smoke и снижает риск занять VRAM во время проверки.
- Для проверки производительности и агентного запуска нужен отдельный сценарий с реальной чат/кодинг-моделью.
- Параметр `-Host` можно использовать как короткий alias для `-HostName`.
