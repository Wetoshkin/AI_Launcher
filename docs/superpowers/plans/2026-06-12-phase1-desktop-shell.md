# AI Launcher Studio — Этап 1: фундамент Desktop-шелла — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans (inline) to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить god-объект `HomeView`/`HomeViewModel` как точку входа на чистый multi-view Shell с боковой навигацией, DPI-корректной тёплой темой, статус-баром железа (заглушка) и страницами-заглушками, чтобы приложение запускалось, красиво выглядело на 150% DPI и навигация реально работала.

**Architecture:** MVVM на Avalonia 12 + CommunityToolkit.Mvvm. `ShellViewModel` держит список `NavigationItem` и `CurrentPage` (ObservableObject). `ShellView` (UserControl) — сетка: слева навигация, снизу статус-бар железа, в центре `ContentControl{Binding CurrentPage}` (страницу рисует существующий `ViewLocator`). Старый `HomeViewModel`/`HomeView` НЕ удаляем в этом этапе — оставляем компилируемыми, чтобы 93 Desktop-теста оставались зелёными; точка входа `App` переключается на `ShellViewModel`. Удаление старого — в финальном этапе чистки.

**Tech Stack:** .NET 8, Avalonia 12.0.3, CommunityToolkit.Mvvm 8.4.1, xUnit.

**Соглашения проекта (важно соблюдать):**
- ViewLocator мапит тип `...ViewModels.XyzViewModel` → `...Views.XyzView` заменой подстроки `ViewModel`→`View`. Имена должны совпадать.
- Компилируемые биндинги включены (`AvaloniaUseCompiledBindingsByDefault=true`) → у каждого View задавать `x:DataType`.
- Тесты — xUnit, проект `tests/Launcher.Desktop.Tests`, уже ссылается на Desktop.
- Команда сборки/тестов: `dotnet build AI-Launcher-Studio.sln --no-restore` и `dotnet test tests/Launcher.Desktop.Tests/Launcher.Desktop.Tests.csproj --no-restore`.

---

### Task 1: Палитра и стили темы (ResourceDictionary)

**Files:**
- Create: `src/Launcher.Desktop/Styles/Theme.axaml`
- Modify: `src/Launcher.Desktop/App.axaml`

- [ ] **Step 1: Создать словарь темы.** Тёплая «бумажная» палитра + базовые стили. Без фиксированных под экран размеров.

`src/Launcher.Desktop/Styles/Theme.axaml`:
```xml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <!-- Палитра -->
  <Color x:Key="PaperColor">#F7EFE3</Color>
  <Color x:Key="SurfaceColor">#FFF9F0</Color>
  <Color x:Key="SurfaceAltColor">#FFF4E5</Color>
  <Color x:Key="BorderColor">#E6CBA6</Color>
  <Color x:Key="AccentColor">#E86F16</Color>
  <Color x:Key="AccentSoftColor">#FFE4BD</Color>
  <Color x:Key="InkColor">#2C241B</Color>
  <Color x:Key="InkSoftColor">#76614C</Color>

  <SolidColorBrush x:Key="PaperBrush" Color="{StaticResource PaperColor}"/>
  <SolidColorBrush x:Key="SurfaceBrush" Color="{StaticResource SurfaceColor}"/>
  <SolidColorBrush x:Key="SurfaceAltBrush" Color="{StaticResource SurfaceAltColor}"/>
  <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource BorderColor}"/>
  <SolidColorBrush x:Key="AccentBrush" Color="{StaticResource AccentColor}"/>
  <SolidColorBrush x:Key="AccentSoftBrush" Color="{StaticResource AccentSoftColor}"/>
  <SolidColorBrush x:Key="InkBrush" Color="{StaticResource InkColor}"/>
  <SolidColorBrush x:Key="InkSoftBrush" Color="{StaticResource InkSoftColor}"/>
</ResourceDictionary>
```

- [ ] **Step 2: Подключить словарь и стили в App.axaml.** Заменить блок `Application.Styles`/добавить ресурсы:

`src/Launcher.Desktop/App.axaml` (полный файл):
```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="Launcher.Desktop.App"
             xmlns:local="using:Launcher.Desktop"
             RequestedThemeVariant="Light">

    <Application.DataTemplates>
        <local:ViewLocator/>
    </Application.DataTemplates>

    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source="avares://Launcher.Desktop/Styles/Theme.axaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>

    <Application.Styles>
        <FluentTheme />
        <Style Selector="TextBlock">
            <Setter Property="Foreground" Value="{DynamicResource InkBrush}"/>
        </Style>
        <Style Selector="Button.nav">
            <Setter Property="HorizontalAlignment" Value="Stretch"/>
            <Setter Property="HorizontalContentAlignment" Value="Left"/>
            <Setter Property="Padding" Value="14,11"/>
            <Setter Property="Margin" Value="0,0,0,4"/>
            <Setter Property="Background" Value="Transparent"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="CornerRadius" Value="10"/>
            <Setter Property="FontSize" Value="14"/>
        </Style>
        <Style Selector="Button.nav:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource SurfaceAltBrush}"/>
        </Style>
        <Style Selector="Button.nav.active /template/ ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource AccentSoftBrush}"/>
        </Style>
        <Style Selector="Button.primary">
            <Setter Property="Background" Value="{DynamicResource AccentBrush}"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="Padding" Value="16,10"/>
            <Setter Property="CornerRadius" Value="10"/>
        </Style>
    </Application.Styles>
</Application>
```

- [ ] **Step 3: Сборка.** Run: `dotnet build src/Launcher.Desktop/Launcher.Desktop.csproj --no-restore`
Expected: Build succeeded, 0 errors. (Тема грузится без падения XAML.)

- [ ] **Step 4: Commit.**
```bash
git add src/Launcher.Desktop/Styles/Theme.axaml src/Launcher.Desktop/App.axaml
git commit -m "feat(desktop): add warm theme resource dictionary"
```

---

### Task 2: Модель элемента навигации

**Files:**
- Create: `src/Launcher.Desktop/Navigation/NavigationItem.cs`
- Test: `tests/Launcher.Desktop.Tests/Navigation/NavigationItemTests.cs`

- [ ] **Step 1: Failing test.**

`tests/Launcher.Desktop.Tests/Navigation/NavigationItemTests.cs`:
```csharp
using Launcher.Desktop.Navigation;
using Launcher.Desktop.ViewModels;

namespace Launcher.Desktop.Tests.Navigation;

public class NavigationItemTests
{
    private sealed class StubPage : ViewModelBase { }

    [Fact]
    public void Carries_title_icon_and_page()
    {
        var page = new StubPage();
        var item = new NavigationItem("Чат", "💬", page);

        Assert.Equal("Чат", item.Title);
        Assert.Equal("💬", item.Icon);
        Assert.Same(page, item.Page);
    }
}
```

- [ ] **Step 2: Run test, verify FAIL.** Run: `dotnet test tests/Launcher.Desktop.Tests/Launcher.Desktop.Tests.csproj --no-restore --filter NavigationItemTests`
Expected: FAIL — `NavigationItem` does not exist (compile error).

- [ ] **Step 3: Implement.**

`src/Launcher.Desktop/Navigation/NavigationItem.cs`:
```csharp
using Launcher.Desktop.ViewModels;

namespace Launcher.Desktop.Navigation;

public sealed record NavigationItem(string Title, string Icon, ViewModelBase Page);
```

- [ ] **Step 4: Run test, verify PASS.** Run: same as Step 2. Expected: PASS.

- [ ] **Step 5: Commit.**
```bash
git add src/Launcher.Desktop/Navigation/NavigationItem.cs tests/Launcher.Desktop.Tests/Navigation/NavigationItemTests.cs
git commit -m "feat(desktop): add navigation item model"
```

---

### Task 3: Страницы-заглушки (page ViewModels + Views)

Создаём чистые страницы. Имена БЕЗ конфликта со старым `HomeViewModel` (новая домашняя — `DashboardViewModel`).

**Files:**
- Create: `src/Launcher.Desktop/ViewModels/Pages/DashboardViewModel.cs`
- Create: `src/Launcher.Desktop/ViewModels/Pages/ChatViewModel.cs`
- Create: `src/Launcher.Desktop/ViewModels/Pages/ModelsViewModel.cs`
- Create: `src/Launcher.Desktop/ViewModels/Pages/RuntimesViewModel.cs`
- Create: `src/Launcher.Desktop/ViewModels/Pages/SettingsViewModel.cs`
- Create: `src/Launcher.Desktop/Views/Pages/DashboardView.axaml` (+ `.axaml.cs`)
- Create: `src/Launcher.Desktop/Views/Pages/ChatView.axaml` (+ `.axaml.cs`)
- Create: `src/Launcher.Desktop/Views/Pages/ModelsView.axaml` (+ `.axaml.cs`)
- Create: `src/Launcher.Desktop/Views/Pages/RuntimesView.axaml` (+ `.axaml.cs`)
- Create: `src/Launcher.Desktop/Views/Pages/SettingsView.axaml` (+ `.axaml.cs`)
- Test: `tests/Launcher.Desktop.Tests/Pages/PageViewModelTests.cs`

> ВАЖНО: ViewLocator мапит по FullName заменой `ViewModel`→`View`. Значит `Launcher.Desktop.ViewModels.Pages.DashboardViewModel` ищет `Launcher.Desktop.Views.Pages.DashboardView`. Namespace VM должен быть `Launcher.Desktop.ViewModels.Pages`, View — `Launcher.Desktop.Views.Pages`. Соблюсти точно.

- [ ] **Step 1: Failing test** (заголовки страниц заданы).

`tests/Launcher.Desktop.Tests/Pages/PageViewModelTests.cs`:
```csharp
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests.Pages;

public class PageViewModelTests
{
    [Fact]
    public void Dashboard_has_title() => Assert.Equal("Главная", new DashboardViewModel().Title);

    [Fact]
    public void Chat_has_title() => Assert.Equal("Чат", new ChatViewModel().Title);

    [Fact]
    public void Models_has_title() => Assert.Equal("Модели", new ModelsViewModel().Title);

    [Fact]
    public void Runtimes_has_title() => Assert.Equal("Среды (runtime)", new RuntimesViewModel().Title);

    [Fact]
    public void Settings_has_title() => Assert.Equal("Настройки", new SettingsViewModel().Title);
}
```

- [ ] **Step 2: Run test, verify FAIL.** Run: `dotnet test tests/Launcher.Desktop.Tests/Launcher.Desktop.Tests.csproj --no-restore --filter PageViewModelTests`
Expected: FAIL — типы не существуют.

- [ ] **Step 3: Implement VMs.** Каждый — `ViewModelBase` с `Title` и кратким `Description`.

`src/Launcher.Desktop/ViewModels/Pages/DashboardViewModel.cs`:
```csharp
namespace Launcher.Desktop.ViewModels.Pages;

public sealed class DashboardViewModel : ViewModelBase
{
    public string Title => "Главная";
    public string Description => "Статус системы и быстрый запуск локальной или онлайн нейросети.";
}
```

`src/Launcher.Desktop/ViewModels/Pages/ChatViewModel.cs`:
```csharp
namespace Launcher.Desktop.ViewModels.Pages;

public sealed class ChatViewModel : ViewModelBase
{
    public string Title => "Чат";
    public string Description => "Здесь появится чат с локальной или онлайн моделью.";
}
```

`src/Launcher.Desktop/ViewModels/Pages/ModelsViewModel.cs`:
```csharp
namespace Launcher.Desktop.ViewModels.Pages;

public sealed class ModelsViewModel : ViewModelBase
{
    public string Title => "Модели";
    public string Description => "Локальный каталог GGUF и поиск моделей на Hugging Face.";
}
```

`src/Launcher.Desktop/ViewModels/Pages/RuntimesViewModel.cs`:
```csharp
namespace Launcher.Desktop.ViewModels.Pages;

public sealed class RuntimesViewModel : ViewModelBase
{
    public string Title => "Среды (runtime)";
    public string Description => "Сборки llama.cpp: определить, скачать, обновить, переключить.";
}
```

`src/Launcher.Desktop/ViewModels/Pages/SettingsViewModel.cs`:
```csharp
namespace Launcher.Desktop.ViewModels.Pages;

public sealed class SettingsViewModel : ViewModelBase
{
    public string Title => "Настройки";
    public string Description => "Прокси, профили, режим Новичок/Эксперт.";
}
```

- [ ] **Step 4: Implement Views.** У всех одинаковый шаблон-заглушка (заголовок + описание по центру). Показать для каждого; различаются `x:Class`, `x:DataType` и namespace.

`src/Launcher.Desktop/Views/Pages/DashboardView.axaml`:
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Launcher.Desktop.ViewModels.Pages"
             x:Class="Launcher.Desktop.Views.Pages.DashboardView"
             x:DataType="vm:DashboardViewModel">
  <StackPanel Margin="32" Spacing="10" VerticalAlignment="Top">
    <TextBlock Text="{Binding Title}" FontSize="28" FontWeight="Bold"/>
    <TextBlock Text="{Binding Description}" FontSize="14" Foreground="{DynamicResource InkSoftBrush}" TextWrapping="Wrap"/>
  </StackPanel>
</UserControl>
```

`src/Launcher.Desktop/Views/Pages/DashboardView.axaml.cs`:
```csharp
using Avalonia.Controls;

namespace Launcher.Desktop.Views.Pages;

public partial class DashboardView : UserControl
{
    public DashboardView() => InitializeComponent();
}
```

Повторить тот же шаблон для `ChatView`, `ModelsView`, `RuntimesView`, `SettingsView` — заменяя `x:Class`, `x:DataType` на соответствующий VM (`ChatViewModel`, `ModelsViewModel`, `RuntimesViewModel`, `SettingsViewModel`) и имя класса в `.axaml.cs`.

ChatView.axaml — как Dashboard, но `x:Class="Launcher.Desktop.Views.Pages.ChatView"` и `x:DataType="vm:ChatViewModel"`. ChatView.axaml.cs — класс `ChatView`.
ModelsView.axaml — `x:Class="...ModelsView"`, `x:DataType="vm:ModelsViewModel"`. .cs — класс `ModelsView`.
RuntimesView.axaml — `x:Class="...RuntimesView"`, `x:DataType="vm:RuntimesViewModel"`. .cs — класс `RuntimesView`.
SettingsView.axaml — `x:Class="...SettingsView"`, `x:DataType="vm:SettingsViewModel"`. .cs — класс `SettingsView`.

- [ ] **Step 5: Run test, verify PASS.** Run: as Step 2. Expected: PASS (5 tests).

- [ ] **Step 6: Build whole solution.** Run: `dotnet build AI-Launcher-Studio.sln --no-restore`. Expected: 0 errors (XAML всех страниц компилируется).

- [ ] **Step 7: Commit.**
```bash
git add src/Launcher.Desktop/ViewModels/Pages src/Launcher.Desktop/Views/Pages tests/Launcher.Desktop.Tests/Pages
git commit -m "feat(desktop): add page view models and stub views"
```

---

### Task 4: ShellViewModel (навигация)

**Files:**
- Create: `src/Launcher.Desktop/ViewModels/ShellViewModel.cs`
- Test: `tests/Launcher.Desktop.Tests/ShellViewModelTests.cs`

- [ ] **Step 1: Failing test.**

`tests/Launcher.Desktop.Tests/ShellViewModelTests.cs`:
```csharp
using System.Linq;
using Launcher.Desktop.Navigation;
using Launcher.Desktop.ViewModels;
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests;

public class ShellViewModelTests
{
    [Fact]
    public void Exposes_five_nav_items()
    {
        var shell = new ShellViewModel();
        Assert.Equal(5, shell.NavigationItems.Count);
        Assert.Equal("Главная", shell.NavigationItems.First().Title);
    }

    [Fact]
    public void Default_page_is_dashboard()
    {
        var shell = new ShellViewModel();
        Assert.IsType<DashboardViewModel>(shell.CurrentPage);
        Assert.Same(shell.NavigationItems.First(), shell.SelectedItem);
    }

    [Fact]
    public void Navigate_changes_current_page_and_selection()
    {
        var shell = new ShellViewModel();
        var chat = shell.NavigationItems.Single(i => i.Title == "Чат");

        shell.NavigateCommand.Execute(chat);

        Assert.IsType<ChatViewModel>(shell.CurrentPage);
        Assert.Same(chat, shell.SelectedItem);
    }
}
```

- [ ] **Step 2: Run test, verify FAIL.** Run: `dotnet test tests/Launcher.Desktop.Tests/Launcher.Desktop.Tests.csproj --no-restore --filter ShellViewModelTests`. Expected: FAIL — `ShellViewModel` отсутствует.

- [ ] **Step 3: Implement.**

`src/Launcher.Desktop/ViewModels/ShellViewModel.cs`:
```csharp
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Navigation;
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private NavigationItem _selectedItem;

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public ShellViewModel()
    {
        NavigationItems = new List<NavigationItem>
        {
            new("Главная", "🏠", new DashboardViewModel()),
            new("Чат", "💬", new ChatViewModel()),
            new("Модели", "📦", new ModelsViewModel()),
            new("Среды (runtime)", "⚙", new RuntimesViewModel()),
            new("Настройки", "🛠", new SettingsViewModel()),
        };

        _selectedItem = NavigationItems[0];
        _currentPage = _selectedItem.Page;
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        SelectedItem = item;
        CurrentPage = item.Page;
    }
}
```

- [ ] **Step 4: Run test, verify PASS.** Run: as Step 2. Expected: PASS (3 tests).

- [ ] **Step 5: Commit.**
```bash
git add src/Launcher.Desktop/ViewModels/ShellViewModel.cs tests/Launcher.Desktop.Tests/ShellViewModelTests.cs
git commit -m "feat(desktop): add shell navigation view model"
```

---

### Task 5: ShellView (layout: nav + контент + статус-бар железа-заглушка)

**Files:**
- Create: `src/Launcher.Desktop/Views/ShellView.axaml` (+ `.axaml.cs`)

- [ ] **Step 1: Implement ShellView.** Сетка: колонка навигации (фикс. MinWidth, не пиксель-хардкод под экран), центральный `ContentControl`, нижний статус-бар (пока статичный текст — реальный probe в Этапе 2).

`src/Launcher.Desktop/Views/ShellView.axaml`:
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:Launcher.Desktop.ViewModels"
             xmlns:nav="using:Launcher.Desktop.Navigation"
             x:Class="Launcher.Desktop.Views.ShellView"
             x:DataType="vm:ShellViewModel">
  <Grid ColumnDefinitions="Auto,*" Background="{DynamicResource PaperBrush}">
    <Border Grid.Column="0" Width="240" Background="{DynamicResource SurfaceBrush}"
            BorderBrush="{DynamicResource BorderBrush}" BorderThickness="0,0,1,0">
      <Grid RowDefinitions="Auto,*,Auto">
        <StackPanel Grid.Row="0" Margin="18,20,18,12" Spacing="2">
          <TextBlock Text="AI Launcher Studio" FontSize="17" FontWeight="Bold"/>
          <TextBlock Text="локально и онлайн" FontSize="12" Foreground="{DynamicResource InkSoftBrush}"/>
        </StackPanel>
        <ItemsControl Grid.Row="1" Margin="10,4" ItemsSource="{Binding NavigationItems}">
          <ItemsControl.ItemTemplate>
            <DataTemplate x:DataType="nav:NavigationItem">
              <Button Classes="nav"
                      Command="{Binding $parent[ItemsControl].((vm:ShellViewModel)DataContext).NavigateCommand}"
                      CommandParameter="{Binding}">
                <StackPanel Orientation="Horizontal" Spacing="10">
                  <TextBlock Text="{Binding Icon}" FontSize="16"/>
                  <TextBlock Text="{Binding Title}" VerticalAlignment="Center"/>
                </StackPanel>
              </Button>
            </DataTemplate>
          </ItemsControl.ItemTemplate>
        </ItemsControl>
        <Border Grid.Row="2" Margin="14" Padding="12,10" CornerRadius="10"
                Background="{DynamicResource SurfaceAltBrush}"
                BorderBrush="{DynamicResource BorderBrush}" BorderThickness="1">
          <StackPanel Spacing="2">
            <TextBlock Text="Железо" FontSize="11" Foreground="{DynamicResource InkSoftBrush}"/>
            <TextBlock Text="определение…" FontSize="12" TextWrapping="Wrap"/>
          </StackPanel>
        </Border>
      </Grid>
    </Border>

    <ContentControl Grid.Column="1" Content="{Binding CurrentPage}"/>
  </Grid>
</UserControl>
```

`src/Launcher.Desktop/Views/ShellView.axaml.cs`:
```csharp
using Avalonia.Controls;

namespace Launcher.Desktop.Views;

public partial class ShellView : UserControl
{
    public ShellView() => InitializeComponent();
}
```

- [ ] **Step 2: Build.** Run: `dotnet build src/Launcher.Desktop/Launcher.Desktop.csproj --no-restore`. Expected: 0 errors.

- [ ] **Step 3: Commit.**
```bash
git add src/Launcher.Desktop/Views/ShellView.axaml src/Launcher.Desktop/Views/ShellView.axaml.cs
git commit -m "feat(desktop): add shell view layout"
```

---

### Task 6: Точка входа на Shell (MainWindow + App)

**Files:**
- Modify: `src/Launcher.Desktop/Views/MainWindow.axaml`
- Modify: `src/Launcher.Desktop/App.axaml.cs`

- [ ] **Step 1: MainWindow → ShellView + ShellViewModel.**

`src/Launcher.Desktop/Views/MainWindow.axaml` (полный файл):
```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:views="using:Launcher.Desktop.Views"
        xmlns:vm="using:Launcher.Desktop.ViewModels"
        x:Class="Launcher.Desktop.Views.MainWindow"
        x:DataType="vm:ShellViewModel"
        Width="1180" Height="760"
        MinWidth="960" MinHeight="600"
        WindowStartupLocation="CenterScreen"
        Background="{DynamicResource PaperBrush}"
        Icon="/Assets/ai-launcher-studio.ico"
        Title="AI Launcher Studio">
  <views:ShellView/>
</Window>
```

- [ ] **Step 2: App.axaml.cs → ShellViewModel.** Заменить тело `OnFrameworkInitializationCompleted` (убрать прямое использование `HomeViewModel` как корня).

`src/Launcher.Desktop/App.axaml.cs` (полный файл):
```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Launcher.Desktop.ViewModels;
using Launcher.Desktop.Views;

namespace Launcher.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new ShellViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
```

> Примечание: старый `HomeViewModel`/`HomeView` и `Services/AvaloniaFilePicker|FolderPicker` остаются в проекте (компилируются), но больше не на старте. Их пере-дом и удаление — в позднем этапе.

- [ ] **Step 3: Build solution + run tests.**
Run: `dotnet build AI-Launcher-Studio.sln --no-restore` → 0 errors.
Run: `dotnet test AI-Launcher-Studio.sln --no-build` → все тесты зелёные (старые 224 + новые ~9).

- [ ] **Step 4: Commit.**
```bash
git add src/Launcher.Desktop/Views/MainWindow.axaml src/Launcher.Desktop/App.axaml.cs
git commit -m "feat(desktop): switch app entry point to shell"
```

---

### Task 7: Визуальная проверка глазами (обязательно)

**Files:** none (ручной прогон + скриншот).

- [ ] **Step 1: Собрать exe.** Run: `dotnet build src/Launcher.Desktop/Launcher.Desktop.csproj --no-restore`.

- [ ] **Step 2: Запустить и снять скриншот.** Запустить `src/Launcher.Desktop/bin/Debug/net8.0/Launcher.Desktop.exe`, подождать ~7с, снять окно (PrintWindow/CopyFromScreen, как делалось при изучении; сохранить в `TestResults/phase1-shell.png`), закрыть процесс.

- [ ] **Step 3: Осмотреть скриншот.** Прочитать PNG (Read tool) и проверить:
  - окно не обрезается на 150% DPI; навигация слева читается; активный пункт подсвечен;
  - центральная страница показывает заголовок «Главная» и описание; шрифты не гигантские;
  - тёплая палитра, нет тёмных артефактов, нет «Not Found:» от ViewLocator.

- [ ] **Step 4: Клик-проверка навигации.** Допустимо программно: переключить `SelectedItem`/выполнить `NavigateCommand` на «Чат», «Модели», «Среды», «Настройки», каждый раз снимая скриншот, убедиться что контент меняется и не ломается. (Либо вручную кликами, если доступно автоматизированное взаимодействие.) Зафиксировать дефекты.

- [ ] **Step 5: Починить найденные дефекты вёрстки** (если есть) минимальными правками XAML/темы и повторить Step 2-3, пока чисто.

- [ ] **Step 6: Commit (если были правки).**
```bash
git add -A
git commit -m "fix(desktop): polish shell layout after visual qa"
```

---

## Self-review (выполнен при написании плана)
- Покрытие спецификации §3 (архитектура шелла, навигация, DPI): Tasks 1-7. Статус-бар железа — заглушка здесь, реальный probe — Этап 2 (спека §8 п.2). Старый god-объект сохранён для зелёных тестов, удаление — финальный этап (спека §2 «Выкидываем» + §8 п.9).
- Плейсхолдеров нет; код приведён полностью; для повторяющихся Views явно указаны различия.
- Согласованность имён: `NavigationItem(Title,Icon,Page)`, `ShellViewModel.NavigateCommand/CurrentPage/SelectedItem/NavigationItems`, страницы `*ViewModel`↔`*View` в namespace `...Pages` — совпадают во всех тасках.

## Следующие этапы
Этапы 2–9 из спецификации (`docs/superpowers/specs/2026-06-12-ai-launcher-studio-redesign-design.md`, §8) планируются отдельными документами по мере достижения, каждый — самостоятельный рабочий срез. Следующий: Этап 2 — мульти-GPU probe + диаграмма памяти (`HardwareMemoryView`), он же заменит заглушку статус-бара железа из Task 5.
