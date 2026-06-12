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
            desktop.MainWindow = new MainWindow
            {
                DataContext = shell,
            };

            var probe = new WmiHardwareProbe(new ProcessCommandRunner());
            _ = shell.LoadHardwareAsync(probe);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
