# Visual QA smoke для AI Launcher Studio

Легкий ручной сценарий проверки GUI после сборки. Он нужен, чтобы быстро поймать регрессии дизайна без Playwright/npm-зависимостей.

## Быстрый запуск

1. Соберите portable-версию:

```powershell
.\package-ai-launcher-studio.bat
```

2. Запустите visual smoke:

```powershell
.\scripts\Invoke-VisualQa.ps1 -ExecutablePath .\publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe
```

Для автоматического smoke-прогона без оставленного окна:

```powershell
.\scripts\Invoke-VisualQa.ps1 `
  -ExecutablePath .\publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe `
  -CloseAfterCapture
```

Для проверки нескольких размеров окна:

```powershell
.\scripts\Invoke-VisualQa.ps1 `
  -ExecutablePath .\publish\AI-Launcher-Studio-win-x64\Launcher.Desktop.exe `
  -WindowSizes 1080x720,1280x720,1440x900 `
  -CloseAfterCapture
```

Скрипт создаст локальную папку `TestResults\visual-qa\<timestamp>\`, сделает screenshot и положит туда `visual-qa-checklist.md`.

## Режимы screenshot

Основной режим - `window capture`: если скрипт сам запустил приложение и Windows вернула `MainWindowHandle` процесса, screenshot сохраняется по границам окна AI Launcher Studio в файл `launcher-window.png`.

Fallback-режим - `fullscreen fallback`: если окно процесса найти не удалось, например приложение не запускалось скриптом, `MainWindowHandle` еще пустой или Windows не вернула bounds окна, скрипт сохраняет весь виртуальный рабочий стол в файл `launcher-fullscreen.png`. Это сохраняет прежнюю совместимость visual smoke.

Фактически использованный режим записывается в `visual-qa-checklist.md` в строке `Capture mode`.

Режим `window matrix` включается параметром `-WindowSizes`. Скрипт меняет размер окна и сохраняет отдельные файлы вида `launcher-window-requested-1280x720-actual-1280x720.png`. Если ОС или `MinWidth`/`MinHeight` приложения не дают выставить запрошенный размер, в имени файла будет виден фактический размер.

## Чеклист

- [ ] Приложение открывается как рабочий GUI AI Launcher Studio, без landing-заглушки.
- [ ] Тема светлая, с теплыми оранжевыми акцентами.
- [ ] Нет случайно вернувшихся темных боковых панелей, карточек или фона.
- [ ] Основные labels на русском языке.
- [ ] Кнопки выбора папок/путей визуально понятны.
- [ ] Две карточки режима выглядят как важный выбор, а не как декоративные панели.
- [ ] Очередь скачивания Hugging Face видна и не ломает layout.
- [ ] Длинные пути модели, runtime и проекта не налезают на соседние кнопки.
- [ ] Текст статусов, ошибок и предупреждений не перекрывает соседние элементы.
- [ ] На размерах `1080x720`, `1280x720` и `1440x900` нет критичных переполнений; если контента много, есть ожидаемый scroll.

## Что приложить к ревью

- `launcher-window.png` или `launcher-fullscreen.png` из папки visual QA.
- Заполненный `visual-qa-checklist.md`.
- Короткую заметку, если screenshot сделан не с publish exe или сработал fullscreen fallback.
