using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Localization;
using Launcher.Desktop.Navigation;
using Launcher.Desktop.Services;
using Launcher.Desktop.ViewModels.Pages;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboard;
    private readonly ModelsViewModel _models = new();
    private readonly RuntimesViewModel _runtimes = new();
    private readonly AgentsViewModel _agents = new();
    private readonly SettingsViewModel _settings = new();

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private NavigationItem _selectedItem;

    [ObservableProperty]
    private string _hardwareSummary = "…";

    private SystemHardware? _hardware;

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public ModelsViewModel Models => _models;
    public RuntimesViewModel Runtimes => _runtimes;
    public AgentsViewModel Agents => _agents;
    public SettingsViewModel Settings => _settings;

    public ShellViewModel()
    {
        _dashboard = new DashboardViewModel();
        _dashboard.RequestNavigate = SelectByKey;

        NavigationItems = new List<NavigationItem>
        {
            new("home", "🏠", _dashboard),
            new("models", "📦", _models),
            new("agents", "🤖", _agents),
            new("runtimes", "⚙", _runtimes),
            new("settings", "🛠", _settings),
        };

        _selectedItem = NavigationItems[0];
        _currentPage = _selectedItem.Page;

        // Применяем сохранённую настройку учёта встройки до загрузки железа.
        GpuSettings.Instance.UseIntegratedGpu = UiPreferences.Load().UseIntegratedGpu;
        GpuSettings.Instance.Changed += (_, _) =>
        {
            if (_hardware is not null)
            {
                HardwareSummary = BuildHardwareSummary(_hardware);
            }
        };

        // Выбор локальной модели в «Моделях» открывает её во вкладке «Агенты».
        _models.UseLocalModel = path =>
        {
            _agents.LocalModelPath = path;
            SelectByKey("agents");
        };

        // Быстрый повтор последнего запуска с Главной.
        _dashboard.RequestQuickLaunch = profile =>
        {
            _agents.ApplyProfile(profile);
            SelectByKey("agents");
            _agents.PrepareAndLaunchCommand.Execute(null);
        };
    }

    public async Task LoadHardwareAsync(IHardwareProbe probe, CancellationToken cancellationToken = default)
    {
        var hardware = await probe.GetHardwareAsync(cancellationToken);
        _hardware = hardware;
        _dashboard.ApplyHardware(hardware);
        _runtimes.ApplyHardware(hardware);
        _agents.ApplyHardware(hardware);
        _models.ApplyHardware(hardware);

        HardwareSummary = BuildHardwareSummary(hardware);
    }

    private static string BuildHardwareSummary(SystemHardware hw)
    {
        var ram = $"ОЗУ: {hw.RamTotalGb:0.0} ГБ";
        if (!hw.HasGpu)
        {
            return $"CPU (без видеокарты)\n{ram}";
        }

        var useIntegrated = GpuSettings.Instance.UseIntegratedGpu;

        // Все карты, самые мощные — первыми; встройку помечаем отдельно.
        var lines = hw.Gpus
            .OrderByDescending(g => g.TotalGb)
            .Select(g =>
            {
                if (GpuClassifier.IsIntegrated(g))
                {
                    var tag = useIntegrated ? " · встройка (вкл.)" : " · встройка (не исп.)";
                    return $"{ShortGpuName(g.Name)} · {g.TotalGb:0.0} ГБ{tag}";
                }

                return $"{ShortGpuName(g.Name)} · {g.TotalGb:0.0} ГБ";
            })
            .ToList();

        var vram = GpuClassifier.UsableVramGb(hw, useIntegrated);
        lines.Add($"Видеопамять для LLM: {vram:0.0} ГБ");
        lines.Add(ram);

        return string.Join("\n", lines);
    }

    private static string ShortGpuName(string name) => name
        .Replace("NVIDIA GeForce ", string.Empty, System.StringComparison.OrdinalIgnoreCase)
        .Replace("NVIDIA ", string.Empty, System.StringComparison.OrdinalIgnoreCase)
        .Replace("(R)", string.Empty).Replace("(TM)", string.Empty)
        .Trim();

    /// <summary>Выбрать вкладку по стабильному ключу (home/chat/models/agents/runtimes/settings).</summary>
    public void SelectByKey(string key)
    {
        foreach (var item in NavigationItems)
        {
            if (string.Equals(item.Key, key, System.StringComparison.OrdinalIgnoreCase))
            {
                SelectedItem = item;
                return;
            }
        }
    }

    partial void OnSelectedItemChanged(NavigationItem value)
    {
        if (value is not null)
        {
            CurrentPage = value.Page;
            if (string.Equals(value.Key, "home", System.StringComparison.OrdinalIgnoreCase))
            {
                _dashboard.LoadQuickLaunch();
            }
            else if (string.Equals(value.Key, "agents", System.StringComparison.OrdinalIgnoreCase))
            {
                // Папку моделей могли указать на вкладке «Модели» — подхватим список.
                _agents.RefreshLocalModels();
            }
        }
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        SelectedItem = item;
    }
}
