using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Localization;
using Launcher.Desktop.Services;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private static readonly string[] ThemeValues = { "Light", "Dark", "System" };
    private static readonly string[] LanguageValues = { "ru", "en" };

    private readonly UiPreferences _prefs;
    private bool _initializing = true;

    [ObservableProperty]
    private int _themeIndex;

    [ObservableProperty]
    private int _languageIndex;

    [ObservableProperty]
    private bool _autoStart;

    [ObservableProperty]
    private bool _useIntegratedGpu;

    [ObservableProperty]
    private string _updateStatus = string.Empty;

    [ObservableProperty]
    private bool _isCheckingUpdate;

    [ObservableProperty]
    private bool _canInstallUpdate;

    [ObservableProperty]
    private bool _isInstallingUpdate;

    private System.Uri? _installerUrl;

    public string Title => Loc.Instance["settings.title"];
    public string AppVersion => "AI Launcher Studio " + AppInfo.Version;

    public SettingsViewModel()
    {
        _prefs = UiPreferences.Load();
        _themeIndex = System.Array.IndexOf(ThemeValues, _prefs.Theme) is var ti && ti >= 0 ? ti : 0;
        _languageIndex = System.Array.IndexOf(LanguageValues, _prefs.Language) is var li && li >= 0 ? li : 0;

        Loc.Instance.Language = LanguageValues[_languageIndex];
        ThemeService.Apply(ThemeValues[_themeIndex]);
        _autoStart = AutoStartService.IsEnabled();
        _useIntegratedGpu = _prefs.UseIntegratedGpu;
        GpuSettings.Instance.UseIntegratedGpu = _prefs.UseIntegratedGpu;
        _initializing = false;
    }

    partial void OnThemeIndexChanged(int value)
    {
        if (_initializing)
        {
            return;
        }

        var theme = ThemeValues[System.Math.Clamp(value, 0, ThemeValues.Length - 1)];
        ThemeService.Apply(theme);
        _prefs.Theme = theme;
        _prefs.Save();
    }

    partial void OnLanguageIndexChanged(int value)
    {
        if (_initializing)
        {
            return;
        }

        var lang = LanguageValues[System.Math.Clamp(value, 0, LanguageValues.Length - 1)];
        Loc.Instance.Language = lang;
        _prefs.Language = lang;
        _prefs.Save();
        OnPropertyChanged(nameof(Title));
    }

    partial void OnAutoStartChanged(bool value)
    {
        if (_initializing)
        {
            return;
        }

        AutoStartService.SetEnabled(value);
    }

    partial void OnUseIntegratedGpuChanged(bool value)
    {
        if (_initializing)
        {
            return;
        }

        GpuSettings.Instance.UseIntegratedGpu = value;
        _prefs.UseIntegratedGpu = value;
        _prefs.Save();
    }

    public string RepoUrl => $"https://github.com/{AppInfo.RepoOwner}/{AppInfo.RepoName}";

    [RelayCommand]
    private void OpenRepo()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(RepoUrl) { UseShellExecute = true });
        }
        catch
        {
            // браузер не открылся
        }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        IsCheckingUpdate = true;
        UpdateStatus = "…";
        CanInstallUpdate = false;
        _installerUrl = null;
        try
        {
            var result = await new AppUpdateService().CheckAsync(CancellationToken.None);
            UpdateStatus = result.Message;
            _installerUrl = result.InstallerUrl;
            CanInstallUpdate = result.HasUpdate && result.InstallerUrl is not null;
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    [RelayCommand]
    private async Task InstallUpdateAsync()
    {
        if (_installerUrl is null || IsInstallingUpdate)
        {
            return;
        }

        IsInstallingUpdate = true;
        try
        {
            var service = new AppUpdateService();
            var progress = new System.Progress<double>(p =>
                UpdateStatus = $"Скачиваю обновление… {p * 100:0}%");

            var installerPath = await service.DownloadInstallerAsync(_installerUrl, progress, CancellationToken.None);

            UpdateStatus = "Запускаю установку. Приложение закроется и автоматически откроется обновлённым.";
            AppUpdateService.LaunchInstaller(installerPath);
            // Выйти по-настоящему, чтобы установщик мог заменить файлы (иначе процесс держит exe).
            await Task.Delay(1500);
            App.ForceExit();
        }
        catch (System.Exception ex)
        {
            UpdateStatus = "Не удалось обновиться автоматически: " + ex.Message + " Скачайте установщик вручную: " + AppInfo.ReleasesUrl;
            IsInstallingUpdate = false;
        }
    }
}
