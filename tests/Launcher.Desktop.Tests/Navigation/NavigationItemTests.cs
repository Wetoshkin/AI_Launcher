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
