using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Localization;
using Launcher.Desktop.Services;
using Launcher.Core.Scenarios;
using Launcher.Runtimes.Compatibility;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Memory;

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
    private string _updateStatus = string.Empty;

    [ObservableProperty]
    private bool _isCheckingUpdate;

    public string Title => Loc.Instance["settings.title"];
    public string AppVersion => "AI Launcher Studio " + AppInfo.Version;

    public IReadOnlyList<ConflictFinding> SampleFindings { get; }

    public SettingsViewModel()
    {
        _prefs = UiPreferences.Load();
        _themeIndex = System.Array.IndexOf(ThemeValues, _prefs.Theme) is var ti && ti >= 0 ? ti : 0;
        _languageIndex = System.Array.IndexOf(LanguageValues, _prefs.Language) is var li && li >= 0 ? li : 0;

        Loc.Instance.Language = LanguageValues[_languageIndex];
        ThemeService.Apply(ThemeValues[_themeIndex]);
        _initializing = false;

        SampleFindings = BuildSampleFindings();
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

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        IsCheckingUpdate = true;
        UpdateStatus = "…";
        try
        {
            var result = await new AppUpdateService().CheckAsync(CancellationToken.None);
            UpdateStatus = result.Message;
        }
        finally
        {
            IsCheckingUpdate = false;
        }
    }

    private static IReadOnlyList<ConflictFinding> BuildSampleFindings()
    {
        var caps = new LlamaServerCapabilities(
            new HashSet<string>(), new HashSet<string>(), new HashSet<string>(),
            SupportsTurboQuant: false, SupportsMtp: true);

        var input = new ConflictCheckInput(
            RuntimeKind: RuntimeKind.LlamaCpp,
            Capabilities: caps,
            Backend: RuntimeBackend.Vulkan,
            HasNvidiaGpu: false,
            Model: new ModelFacts("Qwen2.5 7B", HasMtpHead: false, NativeContextTokens: 32768),
            ContextTokens: 65536,
            KvCache: KvCacheProfile.Symmetric("q8_0"),
            MtpEnabled: true,
            SpeculativeEnabled: false,
            MemoryPlan: null);

        return SettingsConflictEngine.Check(input);
    }
}
