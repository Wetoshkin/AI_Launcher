using System.Collections.Generic;
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
    }

    public async Task LoadHardwareAsync(IHardwareProbe probe, CancellationToken cancellationToken = default)
    {
        var hardware = await probe.GetHardwareAsync(cancellationToken);
        _dashboard.ApplyHardware(hardware);
        _runtimes.ApplyHardware(hardware);
        _chat.ApplyHardware(hardware);

        var gpu = hardware.HasGpu ? hardware.Gpus[0].Name : "CPU";
        HardwareSummary = $"{gpu}\n{hardware.RamTotalGb:0.0} GB RAM";
    }

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
