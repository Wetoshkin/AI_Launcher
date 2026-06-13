using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Launcher.Desktop.ViewModels;
using Launcher.Desktop.Views;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Ports;

namespace Launcher.Desktop;

public partial class App : Application
{
    private bool _reallyExit;

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
            shell.Agents.PickFolderAsync = folderPicker.PickFolderAsync;

            var startPage = Environment.GetEnvironmentVariable("ALS_START_PAGE");
            if (!string.IsNullOrWhiteSpace(startPage))
            {
                shell.SelectByKey(startPage);
            }

            SetupTray(desktop, window);

            // Закрытие окна сворачивает в трей; реальный выход — из меню трея.
            window.Closing += (_, e) =>
            {
                if (!_reallyExit)
                {
                    e.Cancel = true;
                    window.Hide();
                }
            };

            var probe = new WmiHardwareProbe(new ProcessCommandRunner());
            _ = shell.LoadHardwareAsync(probe);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTray(IClassicDesktopStyleApplicationLifetime desktop, Window window)
    {
        void ShowWindow()
        {
            window.Show();
            window.WindowState = WindowState.Normal;
            window.Activate();
        }

        var showItem = new NativeMenuItem("Показать");
        showItem.Click += (_, _) => ShowWindow();

        var quitItem = new NativeMenuItem("Выход");
        quitItem.Click += (_, _) =>
        {
            _reallyExit = true;
            desktop.Shutdown();
        };

        var tray = new TrayIcon
        {
            ToolTipText = "AI Launcher Studio",
            Menu = new NativeMenu { showItem, quitItem },
        };

        try
        {
            tray.Icon = new WindowIcon(AssetLoader.Open(
                new Uri("avares://Launcher.Desktop/Assets/ai-launcher-studio.ico")));
        }
        catch
        {
            // иконка не загрузилась — трей будет без иконки
        }

        tray.Clicked += (_, _) => ShowWindow();

        SetValue(TrayIcon.IconsProperty, new TrayIcons { tray });
    }
}
