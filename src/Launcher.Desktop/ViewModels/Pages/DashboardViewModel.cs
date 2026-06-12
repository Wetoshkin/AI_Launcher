using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Memory;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _hardwareSummary = "Определение железа…";

    [ObservableProperty]
    private DeviceMemoryPlan? _memoryPlan;

    /// <summary>Колбэк навигации: оболочка подставляет переход на вкладку по названию.</summary>
    public Action<string>? RequestNavigate { get; set; }

    public string Title => "Главная";
    public string Description => "Нажмите пару кнопок и начните общаться с нейросетью — локально на своём ПК или онлайн.";

    public string SampleCaption =>
        "Пример раскладки: модель 7B в кванте Q4_K_M при контексте 8K токенов. " +
        "На шаге запуска диаграмма пересчитается под вашу модель и настройки.";

    [RelayCommand]
    private void OpenLocalChat() => RequestNavigate?.Invoke("Чат");

    [RelayCommand]
    private void OpenOnlineChat() => RequestNavigate?.Invoke("Чат");

    [RelayCommand]
    private void OpenModels() => RequestNavigate?.Invoke("Модели");

    [RelayCommand]
    private void OpenRuntimes() => RequestNavigate?.Invoke("Среды (runtime)");

    /// <summary>Применяет реальное железо: краткая сводка + диаграмма для модели-примера.</summary>
    public void ApplyHardware(SystemHardware hardware)
    {
        HardwareSummary = BuildSummary(hardware);

        var sampleModel = new ModelMemorySpec(SizeGb: 4.7, ParametersBillion: 7, NativeContextTokens: 32768);
        var estimate = MemoryEstimator.Estimate(sampleModel, contextTokens: 8192, KvCacheProfile.Symmetric("q8_0"));
        MemoryPlan = DeviceMemoryPlanner.Plan(estimate, hardware);
    }

    private static string BuildSummary(SystemHardware hardware)
    {
        var gpu = hardware.HasGpu
            ? string.Join(", ", hardware.Gpus.Select(g => $"{g.Name} ({g.TotalGb:0.0} ГБ)"))
            : "видеокарта не найдена — будет работать на CPU";

        return $"{hardware.CpuName}\n{gpu}\nОЗУ: {hardware.RamTotalGb:0.0} ГБ " +
               $"(свободно {hardware.RamFreeGb:0.0} ГБ)";
    }
}
