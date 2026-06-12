using System.Linq;
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
