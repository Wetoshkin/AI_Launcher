using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Services;
using Launcher.Models.Catalog;

namespace Launcher.Desktop.ViewModels;

public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly string _defaultModelsDirectory = @"D:\AI\Models";
    private IReadOnlyList<LocalModelFile> _allModels = [];
    private string _modelSearchText = "";

    public HomeViewModel()
    {
        ModelsFolderPath = Directory.Exists(_defaultModelsDirectory)
            ? _defaultModelsDirectory
            : "не указана";
        ProjectsFolderPath = "не указана";
        RefreshLocalModels();
    }

    public string Title => "AI Launcher Studio";

    public string Subtitle => "локальные агенты · сервер моделей · каталог GGUF";

    public string GpuStatus => "GPU: проверка при запуске";

    public string RuntimeStatus => "TurboQuant / MTP: требуется проверка runtime";

    public string PortStatus => "порт 8080: проверить";

    public string ModelsFolderPath { get; private set; }

    public string ProjectsFolderPath { get; private set; }

    public string ModelCountText { get; private set; } = "модели: проверка";

    public string StatusMessage { get; private set; } = "Выберите режим запуска или настройте папки.";

    public IFolderPicker? FolderPicker { get; set; }

    public ObservableCollection<ModelRowViewModel> LocalModels { get; } = [];

    public string ModelSearchText
    {
        get => _modelSearchText;
        set
        {
            if (_modelSearchText == value)
            {
                return;
            }

            _modelSearchText = value;
            OnPropertyChanged();
            ApplyModelFilter();
        }
    }

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
    private async Task ChooseModelsFolderAsync()
    {
        if (FolderPicker is null)
        {
            SetStatus("Выбор папки моделей пока недоступен: окно ещё не готово.");
            return;
        }

        var folder = await FolderPicker.PickFolderAsync("Выберите папку с GGUF-моделями");
        if (string.IsNullOrWhiteSpace(folder))
        {
            SetStatus("Выбор папки моделей отменён.");
            return;
        }

        ModelsFolderPath = folder;
        RefreshLocalModels();
        OnPropertyChanged(nameof(ModelsFolderPath));
        SetStatus("Папка моделей обновлена.");
    }

    [RelayCommand]
    private async Task ChooseProjectsFolderAsync()
    {
        if (FolderPicker is null)
        {
            SetStatus("Выбор папки проектов пока недоступен: окно ещё не готово.");
            return;
        }

        var folder = await FolderPicker.PickFolderAsync("Выберите папку с проектами");
        if (string.IsNullOrWhiteSpace(folder))
        {
            SetStatus("Выбор папки проектов отменён.");
            return;
        }

        ProjectsFolderPath = folder;
        OnPropertyChanged(nameof(ProjectsFolderPath));
        SetStatus("Папка проектов обновлена.");
    }

    [RelayCommand]
    private void CheckPort()
    {
        SetStatus("Проверка порта использует WindowsPortInspector; llama-server можно будет освободить безопасно.");
    }

    [RelayCommand]
    private void RefreshModels()
    {
        RefreshLocalModels();
        SetStatus("Каталог локальных моделей обновлён.");
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void RefreshLocalModels()
    {
        _allModels = Directory.Exists(ModelsFolderPath)
            ? LocalModelCatalog.Scan([ModelsFolderPath])
            : [];
        ModelCountText = _allModels.Count == 0
            ? "модели: GGUF не найдены"
            : $"модели: {_allModels.Count} GGUF";
        OnPropertyChanged(nameof(ModelCountText));
        ApplyModelFilter();
    }

    private void ApplyModelFilter()
    {
        LocalModels.Clear();
        foreach (var model in ModelFilterService.Apply(_allModels, new ModelFilter(null, null, null, ModelSearchText)).Take(8))
        {
            LocalModels.Add(new ModelRowViewModel(model));
        }
    }
}
