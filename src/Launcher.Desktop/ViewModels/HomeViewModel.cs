using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Launcher.Core.Scenarios;
using Launcher.Desktop.Services;
using Launcher.Models.Catalog;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly string _defaultModelsDirectory = @"D:\AI\Models";
    private readonly HuggingFaceModelClient _huggingFaceClient;
    private IReadOnlyList<LocalModelFile> _allModels = [];
    private LaunchWizardState _wizardState;
    private string _modelSearchText = "";
    private string _hfSearchText = "qwen coder gguf";
    private HuggingFaceSort _hfSort = HuggingFaceSort.Downloads;

    public HomeViewModel()
        : this(new HuggingFaceModelClient(new HttpClient
        {
            BaseAddress = new System.Uri("https://huggingface.co")
        }))
    {
    }

    public HomeViewModel(HuggingFaceModelClient huggingFaceClient)
    {
        _huggingFaceClient = huggingFaceClient;
        _wizardState = LaunchWizardState.ForScenario(new LaunchScenario(
            LaunchMode.Agent,
            AgentKind.Kilo,
            RuntimeKind.LlamaCppTurboQuant));
        ModelsFolderPath = Directory.Exists(_defaultModelsDirectory)
            ? _defaultModelsDirectory
            : "не указана";
        ProjectsFolderPath = "не указана";
        RefreshLocalModels();
        RefreshWizardSteps();
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

    public ObservableCollection<RemoteModelRowViewModel> RemoteModels { get; } = [];

    public ObservableCollection<WizardStepRowViewModel> WizardSteps { get; } = [];

    public string CurrentWizardStepText => StepLabel(_wizardState.CurrentStep);

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

    public string HfSearchText
    {
        get => _hfSearchText;
        set
        {
            if (_hfSearchText == value)
            {
                return;
            }

            _hfSearchText = value;
            OnPropertyChanged();
        }
    }

    public HuggingFaceSort HfSort
    {
        get => _hfSort;
        set
        {
            if (_hfSort == value)
            {
                return;
            }

            _hfSort = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<HuggingFaceSort> HfSortOptions { get; } =
    [
        HuggingFaceSort.Downloads,
        HuggingFaceSort.Likes,
        HuggingFaceSort.LastModified,
        HuggingFaceSort.Trending
    ];

    public ObservableCollection<string> Presets { get; } =
    [
        "Kilo · Qwen3 Coder · TurboQuant · 64k",
        "OpenCode · Gemma · Ollama",
        "Endpoint · Hermes · MTP · 8081"
    ];

    [RelayCommand]
    private void SelectAgentMode()
    {
        SetScenario(new LaunchScenario(LaunchMode.Agent, AgentKind.Kilo, RuntimeKind.LlamaCppTurboQuant));
        SetStatus("Режим проекта: далее выбор папки проекта, агента, модели и runtime.");
    }

    [RelayCommand]
    private void SelectEndpointMode()
    {
        SetScenario(new LaunchScenario(LaunchMode.Endpoint, AgentKind.None, RuntimeKind.LlamaCppMtp));
        SetStatus("Режим сервера: далее выбор модели, контекста, KV/MTP и порта.");
    }

    [RelayCommand]
    private void NextStep()
    {
        _wizardState = _wizardState.Next();
        RefreshWizardSteps();
        SetStatus($"Текущий шаг: {CurrentWizardStepText}.");
    }

    [RelayCommand]
    private void PreviousStep()
    {
        _wizardState = _wizardState.Back();
        RefreshWizardSteps();
        SetStatus($"Текущий шаг: {CurrentWizardStepText}.");
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

    [RelayCommand]
    private async Task SearchHuggingFaceAsync()
    {
        if (string.IsNullOrWhiteSpace(HfSearchText))
        {
            SetStatus("Введите запрос для Hugging Face.");
            return;
        }

        SetStatus("Ищу GGUF-модели на Hugging Face...");
        try
        {
            var models = await _huggingFaceClient.SearchAsync(
                new HuggingFaceModelSearchRequest(HfSearchText, HfSort, Limit: 10, GgufOnly: true),
                default);

            RemoteModels.Clear();
            foreach (var model in models)
            {
                RemoteModels.Add(new RemoteModelRowViewModel(model));
            }

            SetStatus(models.Count == 0
                ? "Hugging Face не вернул GGUF-моделей по этому запросу."
                : $"Hugging Face: найдено {models.Count} моделей.");
        }
        catch (HttpRequestException ex)
        {
            SetStatus($"Hugging Face недоступен: {ex.Message}");
        }
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void SetScenario(LaunchScenario scenario)
    {
        _wizardState = LaunchWizardState.ForScenario(scenario);
        RefreshWizardSteps();
    }

    private void RefreshWizardSteps()
    {
        WizardSteps.Clear();
        for (var index = 0; index < _wizardState.Route.Count; index++)
        {
            var step = _wizardState.Route[index];
            WizardSteps.Add(new WizardStepRowViewModel(
                (index + 1).ToString(),
                StepLabel(step),
                index == _wizardState.CurrentIndex));
        }

        OnPropertyChanged(nameof(CurrentWizardStepText));
    }

    private static string StepLabel(WizardStep step) => step switch
    {
        WizardStep.Mode => "Режим",
        WizardStep.Project => "Проект",
        WizardStep.Agent => "Агент",
        WizardStep.Model => "Модель",
        WizardStep.Runtime => "Runtime",
        WizardStep.Port => "Порт",
        WizardStep.KvMtpContext => "KV / MTP / контекст",
        WizardStep.AgentOptions => "Опции агента",
        WizardStep.Review => "Проверка",
        WizardStep.Launch => "Запуск",
        _ => step.ToString()
    };

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
