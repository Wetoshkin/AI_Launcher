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
