using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using Launcher.Models.Catalog;

namespace Launcher.Desktop.ViewModels;

public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly string _defaultModelsDirectory = @"D:\AI\Models";

    public HomeViewModel()
    {
        ModelsFolderPath = Directory.Exists(_defaultModelsDirectory)
            ? _defaultModelsDirectory
            : "не указана";
        ProjectsFolderPath = "не указана";
        ModelCountText = CountLocalModelsText(ModelsFolderPath);
    }

    public string Title => "AI Launcher Studio";

    public string Subtitle => "локальные агенты · сервер моделей · каталог GGUF";

    public string GpuStatus => "GPU: проверка при запуске";

    public string RuntimeStatus => "TurboQuant / MTP: требуется проверка runtime";

    public string PortStatus => "порт 8080: проверить";

    public string ModelsFolderPath { get; private set; }

    public string ProjectsFolderPath { get; private set; }

    public string ModelCountText { get; private set; }

    public string StatusMessage { get; private set; } = "Выберите режим запуска или настройте папки.";

    public ObservableCollection<string> Presets { get; } =
    [
        "Kilo · Qwen3 Coder · TurboQuant · 64k",
        "OpenCode · Gemma · Ollama",
        "Endpoint · Hermes · MTP · 8081"
    ];

    [RelayCommand]
    private void SelectAgentMode()
    {
        StatusMessage = "Режим проекта: далее выбор папки проекта, агента, модели и runtime.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    [RelayCommand]
    private void SelectEndpointMode()
    {
        StatusMessage = "Режим сервера: далее выбор модели, контекста, KV/MTP и порта.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    [RelayCommand]
    private void ChooseModelsFolder()
    {
        StatusMessage = "Здесь будет системный выбор папки моделей.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    [RelayCommand]
    private void ChooseProjectsFolder()
    {
        StatusMessage = "Здесь будет системный выбор папки проектов.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    [RelayCommand]
    private void CheckPort()
    {
        StatusMessage = "Проверка порта использует WindowsPortInspector; llama-server можно будет освободить безопасно.";
        OnPropertyChanged(nameof(StatusMessage));
    }

    private static string CountLocalModelsText(string modelsFolderPath)
    {
        if (!Directory.Exists(modelsFolderPath))
        {
            return "модели: папка не выбрана";
        }

        var count = LocalModelCatalog.Scan([modelsFolderPath]).Count;
        return count == 0 ? "модели: GGUF не найдены" : $"модели: {count} GGUF";
    }
}
