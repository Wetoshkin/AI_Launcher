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
