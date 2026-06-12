using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Desktop.Localization;
using Launcher.Desktop.ViewModels;

namespace Launcher.Desktop.Navigation;

/// <summary>
/// Пункт навигации. <see cref="Key"/> стабилен (для выбора и deep-link), <see cref="Title"/> локализован
/// и обновляется при смене языка.
/// </summary>
public sealed partial class NavigationItem : ObservableObject
{
    public string Key { get; }
    public string Icon { get; }
    public ViewModelBase Page { get; }

    public string Title => Loc.Instance[$"nav.{Key}"];

    public NavigationItem(string key, string icon, ViewModelBase page)
    {
        Key = key;
        Icon = icon;
        Page = page;
        Loc.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Title));
    }
}
