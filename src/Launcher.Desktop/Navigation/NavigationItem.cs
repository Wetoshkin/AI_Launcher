using Launcher.Desktop.ViewModels;

namespace Launcher.Desktop.Navigation;

public sealed record NavigationItem(string Title, string Icon, ViewModelBase Page);
