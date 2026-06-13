using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Services;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _hasQuickLaunch;

    [ObservableProperty]
    private string _quickLaunchSummary = string.Empty;

    /// <summary>Колбэк навигации: оболочка подставляет переход на вкладку по ключу.</summary>
    public Action<string>? RequestNavigate { get; set; }

    /// <summary>Колбэк быстрого повтора последнего запуска (оболочка применяет профиль и стартует).</summary>
    public Action<LaunchProfile>? RequestQuickLaunch { get; set; }

    /// <summary>Показывать онбординг, если ещё не установлен ни один runtime.</summary>
    public bool ShowOnboarding => Services.LocalServerLauncher.FindInstalledRuntime() is null;

    public string Title => "Главная";
    public string Description => "Нажмите пару кнопок и начните общаться с нейросетью — локально на своём ПК или онлайн.";

    public DashboardViewModel()
    {
        LoadQuickLaunch();
    }

    /// <summary>Перечитывает сохранённый профиль (например, после возврата на Главную).</summary>
    public void LoadQuickLaunch()
    {
        var last = UiPreferences.Load().LastLaunch;
        if (last is { HasValue: true })
        {
            HasQuickLaunch = true;
            QuickLaunchSummary = last.Summary;
        }
        else
        {
            HasQuickLaunch = false;
            QuickLaunchSummary = string.Empty;
        }
    }

    [RelayCommand]
    private void QuickLaunch()
    {
        var last = UiPreferences.Load().LastLaunch;
        if (last is { HasValue: true })
        {
            RequestQuickLaunch?.Invoke(last);
        }
    }

    [RelayCommand]
    private void OpenAgents() => RequestNavigate?.Invoke("agents");

    [RelayCommand]
    private void OpenModels() => RequestNavigate?.Invoke("models");

    [RelayCommand]
    private void OpenRuntimes() => RequestNavigate?.Invoke("runtimes");

    /// <summary>Железо показывается в боковой панели; здесь оно не дублируется.</summary>
    public void ApplyHardware(SystemHardware hardware)
    {
    }
}
