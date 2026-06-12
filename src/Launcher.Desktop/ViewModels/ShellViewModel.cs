using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Navigation;
using Launcher.Desktop.ViewModels.Pages;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels;

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboard;

    [ObservableProperty]
    private ViewModelBase _currentPage;

    [ObservableProperty]
    private NavigationItem _selectedItem;

    [ObservableProperty]
    private string _hardwareSummary = "определение…";

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public ShellViewModel()
    {
        _dashboard = new DashboardViewModel();

        NavigationItems = new List<NavigationItem>
        {
            new("Главная", "🏠", _dashboard),
            new("Чат", "💬", new ChatViewModel()),
            new("Модели", "📦", new ModelsViewModel()),
            new("Среды (runtime)", "⚙", new RuntimesViewModel()),
            new("Настройки", "🛠", new SettingsViewModel()),
        };

        _selectedItem = NavigationItems[0];
        _currentPage = _selectedItem.Page;
    }

    /// <summary>
    /// Загружает железо в фоне и обновляет сводку в сайдбаре и диаграмму на главной.
    /// Вызывается из App, а не из конструктора, чтобы тесты не запускали внешние процессы.
    /// </summary>
    public async Task LoadHardwareAsync(IHardwareProbe probe, CancellationToken cancellationToken = default)
    {
        var hardware = await probe.GetHardwareAsync(cancellationToken);
        _dashboard.ApplyHardware(hardware);

        var gpu = hardware.HasGpu
            ? hardware.Gpus[0].Name
            : "CPU (без видеокарты)";
        HardwareSummary = $"{gpu}\nОЗУ: {hardware.RamTotalGb:0.0} ГБ";
    }

    /// <summary>Выбрать вкладку по заголовку (для deep-link/QA). Тихо игнорирует неизвестный.</summary>
    public void SelectByTitle(string title)
    {
        foreach (var item in NavigationItems)
        {
            if (string.Equals(item.Title, title, System.StringComparison.OrdinalIgnoreCase))
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
