using System.Linq;
using Launcher.Desktop.ViewModels;
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests;

public class ShellViewModelTests
{
    [Fact]
    public void Exposes_all_nav_items()
    {
        var shell = new ShellViewModel();
        Assert.Equal(5, shell.NavigationItems.Count);
        Assert.Equal("home", shell.NavigationItems.First().Key);
        Assert.Contains(shell.NavigationItems, i => i.Key == "agents");
        Assert.DoesNotContain(shell.NavigationItems, i => i.Key == "chat");
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
        var agents = shell.NavigationItems.Single(i => i.Key == "agents");

        shell.NavigateCommand.Execute(agents);

        Assert.IsType<AgentsViewModel>(shell.CurrentPage);
        Assert.Same(agents, shell.SelectedItem);
    }

    [Fact]
    public void Select_by_key_switches_page()
    {
        var shell = new ShellViewModel();
        shell.SelectByKey("models");
        Assert.IsType<ModelsViewModel>(shell.CurrentPage);
    }
}
