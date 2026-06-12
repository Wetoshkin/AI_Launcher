using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Launcher.Desktop.ViewModels;
using Launcher.Desktop.Views;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Ports;

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
            var shell = new ShellViewModel();
            var window = new MainWindow
            {
                DataContext = shell,
            };
            desktop.MainWindow = window;

            var folderPicker = new Services.AvaloniaFolderPicker(window);
            shell.Models.PickFolderAsync = folderPicker.PickFolderAsync;

            var filePicker = new Services.AvaloniaFilePicker(window);
            shell.Chat.PickModelAsync = filePicker.PickFileAsync;

            var startPage = System.Environment.GetEnvironmentVariable("ALS_START_PAGE");
            if (!string.IsNullOrWhiteSpace(startPage))
            {
                shell.SelectByTitle(startPage);
            }

            var probe = new WmiHardwareProbe(new ProcessCommandRunner());
            _ = shell.LoadHardwareAsync(probe);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
