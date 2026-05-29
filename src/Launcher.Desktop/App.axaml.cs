using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Launcher.Desktop.Services;
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
            var viewModel = new HomeViewModel();
            var window = new MainWindow
            {
                DataContext = viewModel,
            };
            viewModel.FolderPicker = new AvaloniaFolderPicker(window);
            viewModel.FilePicker = new AvaloniaFilePicker(window);
            _ = viewModel.LoadSettingsCommand.ExecuteAsync(null);
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
