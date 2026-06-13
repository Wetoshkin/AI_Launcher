using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Localization;
using Launcher.Desktop.Navigation;
using Launcher.Desktop.ViewModels.Pages;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboard;
    private readonly ChatViewModel _chat = new();
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

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public ChatViewModel Chat => _chat;
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
            new("chat", "💬", _chat),
            new("models", "📦", _models),
            new("agents", "🤖", _agents),
            new("runtimes", "⚙", _runtimes),
            new("settings", "🛠", _settings),
        };

        _selectedItem = NavigationItems[0];
        _currentPage = _selectedItem.Page;

        // Выбор локальной модели в «Моделях» открывает её в «Чате».
        _models.UseLocalModel = path =>
        {
            _chat.LocalModelPath = path;
            SelectByKey("chat");
        };
    }

    public async Task LoadHardwareAsync(IHardwareProbe probe, CancellationToken cancellationToken = default)
    {
        var hardware = await probe.GetHardwareAsync(cancellationToken);
        _dashboard.ApplyHardware(hardware);
        _runtimes.ApplyHardware(hardware);
        _chat.ApplyHardware(hardware);
        _models.ApplyHardware(hardware);

        HardwareSummary = BuildHardwareSummary(hardware);
    }

    private static string BuildHardwareSummary(Launcher.Runtimes.Hardware.SystemHardware hw)
    {
        var ram = $"ОЗУ: {hw.RamTotalGb:0.0} ГБ";
        if (!hw.HasGpu)
        {
            return $"CPU (без видеокарты)\n{ram}";
        }

        // Самые мощные карты — первыми (по объёму видеопамяти).
        var top = hw.Gpus
            .OrderByDescending(g => g.TotalGb)
            .Take(2)
            .Select(g => $"{ShortGpuName(g.Name)} · {g.TotalGb:0.0} ГБ")
            .ToList();

        if (hw.Gpus.Count > 2)
        {
            top.Add($"…и ещё {hw.Gpus.Count - 2}");
        }

        return string.Join("\n", top) + "\n" + ram;
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
        }
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        SelectedItem = item;
    }
}
