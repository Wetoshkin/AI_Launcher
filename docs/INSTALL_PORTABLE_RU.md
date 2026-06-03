# Установка portable zip

AI Launcher Studio можно запускать как portable-приложение: без installer framework и без прав администратора. Установочный скрипт распаковывает zip, проверяет `.sha256`, если он лежит рядом, и при необходимости создаёт ярлыки.

## Скачать архив

1. Откройте страницу GitHub Release или успешный GitHub Actions run workflow `Package`. Tag-сборки `v*` публикуют Release автоматически; ручной запуск workflow может подготовить artifact или опубликовать Release, если включить `publish_release` и указать `release_tag`.
2. Скачайте portable artifact/zip для Windows x64:

```text
AI-Launcher-Studio-win-x64.zip
```

Для tag-сборок имя artifact может включать версию, например:

```text
AI-Launcher-Studio-v1.0.0-win-x64
```

Внутри portable artifact лежат zip и checksum с тем же basename. Для локальной сборки это:

```text
AI-Launcher-Studio-win-x64.zip
AI-Launcher-Studio-win-x64.zip.sha256
```

Для tag/manual GitHub Actions сборок имена будут версионированными, например:

```text
AI-Launcher-Studio-v1.0.0-win-x64.zip
AI-Launcher-Studio-v1.0.0-win-x64.zip.sha256
```

## Проверить SHA256

Если рядом с zip есть файл `.sha256`, проверьте архив перед распаковкой:

```powershell
Get-FileHash .\AI-Launcher-Studio-win-x64.zip -Algorithm SHA256
Get-Content .\AI-Launcher-Studio-win-x64.zip.sha256
```

Значение `Hash` из `Get-FileHash` должно совпадать с хешем в `.sha256`.

## Распаковать и запустить вручную

Рекомендуемый путь установки:

```text
D:\AI\AI-Launcher-Studio
```

Можно выбрать и другую папку, если у пользователя есть права на запись.

```powershell
New-Item -ItemType Directory -Force D:\AI\AI-Launcher-Studio
Expand-Archive .\AI-Launcher-Studio-win-x64.zip -DestinationPath D:\AI\AI-Launcher-Studio -Force
D:\AI\AI-Launcher-Studio\Launcher.Desktop.exe
```

Если Windows показывает предупреждение SmartScreen, проверьте, что архив скачан из ожидаемого GitHub Actions run или Release, затем выберите запуск вручную.

## Распаковать через скрипт

Скрипт не требует прав администратора. Он проверяет `.sha256`, если файл лежит рядом с zip, распаковывает архив и печатает путь к `Launcher.Desktop.exe`.

```powershell
.\scripts\Install-PortablePackage.ps1 `
  -ZipPath .\AI-Launcher-Studio-win-x64.zip `
  -Destination D:\AI\AI-Launcher-Studio
```

Если `.sha256` рядом с zip отсутствует, скрипт продолжит установку и явно напишет, что checksum не проверялся.

## Создать ярлыки

Portable-установка может создать ярлык на рабочем столе и в меню Пуск:

```powershell
.\scripts\Install-PortablePackage.ps1 `
  -ZipPath .\AI-Launcher-Studio-win-x64.zip `
  -Destination D:\AI\AI-Launcher-Studio `
  -CreateDesktopShortcut `
  -CreateStartMenuShortcut
```

Имя ярлыка можно поменять:

```powershell
.\scripts\Install-PortablePackage.ps1 `
  -ZipPath .\AI-Launcher-Studio-win-x64.zip `
  -Destination D:\AI\AI-Launcher-Studio `
  -CreateStartMenuShortcut `
  -ShortcutName "AI Launcher Studio"
```

## Обновление portable-папки

Для обновления скачайте новый zip и распакуйте его в ту же папку с `-Force` или тем же скриптом. Перед обновлением закройте `Launcher.Desktop.exe`, иначе Windows может заблокировать замену файлов.
