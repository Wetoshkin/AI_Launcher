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

Скрипт создаст локальную папку `TestResults\visual-qa\<timestamp>\`, сделает fullscreen screenshot и положит туда `visual-qa-checklist.md`.

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
- [ ] На размере около `1280x720` нет критичных переполнений; если контента много, есть ожидаемый scroll.

## Что приложить к ревью

- `launcher-fullscreen.png` из папки visual QA.
- Заполненный `visual-qa-checklist.md`.
- Короткую заметку, если screenshot сделан не с publish exe, а с уже открытого окна.
