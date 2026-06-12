using Launcher.Desktop.Localization;
using Launcher.Desktop.Navigation;
using Launcher.Desktop.ViewModels;

namespace Launcher.Desktop.Tests.Navigation;

public class NavigationItemTests
{
    private sealed class StubPage : ViewModelBase { }

    [Fact]
    public void Carries_key_icon_page_and_localized_title()
    {
        Loc.Instance.Language = "ru";
        var page = new StubPage();
        var item = new NavigationItem("chat", "💬", page);

        Assert.Equal("chat", item.Key);
        Assert.Equal("💬", item.Icon);
        Assert.Same(page, item.Page);
        Assert.Equal("Чат", item.Title);
    }

    [Fact]
    public void Title_follows_language()
    {
        var item = new NavigationItem("home", "🏠", new StubPage());

        Loc.Instance.Language = "en";
        Assert.Equal("Home", item.Title);

        Loc.Instance.Language = "ru";
        Assert.Equal("Главная", item.Title);
    }
}
