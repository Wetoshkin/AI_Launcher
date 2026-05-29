using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Launcher.Agents.Commands;
using Launcher.Agents.Discovery;
using Launcher.Core.Decoding;
using Launcher.Core.Guards;
using Launcher.Core.LaunchPlans;
using Launcher.Core.Profiles;
using Launcher.Core.Review;
using Launcher.Core.Scenarios;
using Launcher.Desktop.Services;
using Launcher.Models.Catalog;
using Launcher.Models.HuggingFace;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;
using Launcher.Runtimes.Startup;
using Launcher.Runtimes.Status;

namespace Launcher.Desktop.ViewModels;

public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly string _defaultModelsDirectory = @"D:\AI\Models";
    private readonly HuggingFaceModelClient _huggingFaceClient;
    private readonly IHuggingFaceModelDownloadService _modelDownloadService;
    private readonly AgentCliCatalogService _agentCliCatalogService;
    private readonly IRuntimePackageInstaller _runtimePackageInstaller;
    private readonly ILauncherSettingsStore? _settingsStore;
    private readonly RuntimeDashboardService _runtimeDashboardService;
    private readonly RuntimeStartCoordinator _runtimeStartCoordinator;
    private LaunchPlan? _lastLaunchPlan;
    private IReadOnlyList<LocalModelFile> _allModels = [];
    private LaunchWizardState _wizardState;
    private string _modelSearchText = "";
    private string _hfSearchText = "qwen coder gguf";
    private string _runtimeArchivePath = "";
    private string _runtimeRootPath = @"D:\AI\runtimes";
    private HuggingFaceSort _hfSort = HuggingFaceSort.Downloads;
    private int _port = 8080;
    private int _contextTokens = 65536;
    private DecodingPresetRowViewModel _selectedDecodingPreset;
    private ModelRowViewModel? _selectedLocalModel;
    private PresetRowViewModel? _selectedPreset;
    private RemoteModelRowViewModel? _selectedRemoteModel;
    private RemoteGgufDownloadOptionRowViewModel? _selectedRemoteDownloadOption;
    private bool _isDownloading;
    private CancellationTokenSource? _downloadCancellation;
    private LlamaRuntimeInfo? _bestRuntime;
    private AgentKind _selectedAgent = AgentKind.Kilo;
    private RuntimeKind _selectedRuntime = RuntimeKind.LlamaCppTurboQuant;

    public HomeViewModel()
        : this(new HuggingFaceModelClient(new HttpClient
        {
            BaseAddress = new System.Uri("https://huggingface.co")
        }),
        new RuntimeDashboardService(
            new NvidiaSmiGpuProbe(new ProcessCommandRunner()),
            new WindowsPortInspector(),
            new RuntimeCatalogService(new ProcessCommandRunner())),
            new RuntimeStartCoordinator(
            new WindowsPortInspector(),
            new PortReleaseService(new ProcessCommandRunner()),
            new ProcessStarter(),
            new OpenAiEndpointHealthClient(new HttpClient())),
        new HuggingFaceModelDownloadService(new HttpClient()),
        new LauncherSettingsFileStore(DefaultSettingsPath()),
        new AgentCliCatalogService(new WindowsExecutableResolver()),
        new RuntimePackageInstaller())
    {
    }

    public HomeViewModel(
        HuggingFaceModelClient huggingFaceClient,
        RuntimeDashboardService runtimeDashboardService,
        RuntimeStartCoordinator runtimeStartCoordinator,
        IHuggingFaceModelDownloadService modelDownloadService,
        ILauncherSettingsStore? settingsStore = null,
        AgentCliCatalogService? agentCliCatalogService = null,
        IRuntimePackageInstaller? runtimePackageInstaller = null)
    {
        _huggingFaceClient = huggingFaceClient;
        _modelDownloadService = modelDownloadService;
        _agentCliCatalogService = agentCliCatalogService ?? new AgentCliCatalogService(new WindowsExecutableResolver());
        _runtimePackageInstaller = runtimePackageInstaller ?? new RuntimePackageInstaller();
        _settingsStore = settingsStore;
        _runtimeDashboardService = runtimeDashboardService;
        _runtimeStartCoordinator = runtimeStartCoordinator;
        _wizardState = LaunchWizardState.ForScenario(new LaunchScenario(
            LaunchMode.Agent,
            AgentKind.Kilo,
            RuntimeKind.LlamaCppTurboQuant));
        DecodingPresets = new ObservableCollection<DecodingPresetRowViewModel>(
            DecodingPresetCatalog.All.Select(preset => new DecodingPresetRowViewModel(preset)));
        Presets = new ObservableCollection<PresetRowViewModel>(DefaultPresets());
        _selectedPreset = Presets.FirstOrDefault();
        _selectedDecodingPreset = DecodingPresets[0];
        ModelsFolderPath = Directory.Exists(_defaultModelsDirectory)
            ? _defaultModelsDirectory
            : "не указана";
        ProjectsFolderPath = "не указана";
        RefreshLocalModels();
        RefreshWizardSteps();
    }

    public string Title => "AI Launcher Studio";

    public string Subtitle => "локальные агенты · сервер моделей · каталог GGUF";

    public string GpuStatus { get; private set; } = "GPU: проверить";

    public string RuntimeStatus { get; private set; } = "runtime: требуется проверка";

    public string PortStatus { get; private set; } = "порт 8080: проверить";

    public string ModelsFolderPath { get; private set; }

    public string ProjectsFolderPath { get; private set; }

    public string ModelCountText { get; private set; } = "модели: проверка";

    public string StatusMessage { get; private set; } = "Выберите режим запуска или настройте папки.";

    public string RuntimeArchivePath
    {
        get => _runtimeArchivePath;
        set
        {
            if (_runtimeArchivePath == value)
            {
                return;
            }

            _runtimeArchivePath = value;
            OnPropertyChanged();
        }
    }

    public string RuntimeRootPath
    {
        get => _runtimeRootPath;
        set
        {
            if (_runtimeRootPath == value)
            {
                return;
            }

            _runtimeRootPath = value;
            OnPropertyChanged();
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            var normalized = Math.Clamp(value, 1, 65535);
            if (_port == normalized)
            {
                return;
            }

            _port = normalized;
            OnPropertyChanged();
            _lastLaunchPlan = null;
        }
    }

    public int ContextTokens
    {
        get => _contextTokens;
        set
        {
            var normalized = Math.Clamp(value, 1024, 1_048_576);
            if (_contextTokens == normalized)
            {
                return;
            }

            _contextTokens = normalized;
            OnPropertyChanged();
            RefreshLaunchReview();
            _lastLaunchPlan = null;
        }
    }

    public string DownloadProgressText { get; private set; } = "Загрузок нет.";

    public double DownloadProgressPercent { get; private set; }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (_isDownloading == value)
            {
                return;
            }

            _isDownloading = value;
            OnPropertyChanged();
        }
    }

    public IFolderPicker? FolderPicker { get; set; }

    public ObservableCollection<ModelRowViewModel> LocalModels { get; } = [];

    public ModelRowViewModel? SelectedLocalModel
    {
        get => _selectedLocalModel;
        set
        {
            if (_selectedLocalModel == value)
            {
                return;
            }

            _selectedLocalModel = value;
            OnPropertyChanged();
            RefreshLaunchReview();
            _lastLaunchPlan = null;
        }
    }

    public ObservableCollection<RemoteModelRowViewModel> RemoteModels { get; } = [];

    public RemoteModelRowViewModel? SelectedRemoteModel
    {
        get => _selectedRemoteModel;
        set
        {
            if (_selectedRemoteModel == value)
            {
                return;
            }

            _selectedRemoteModel = value;
            OnPropertyChanged();
            RefreshRemoteDownloadOptions();
        }
    }

    public ObservableCollection<RemoteGgufDownloadOptionRowViewModel> RemoteDownloadOptions { get; } = [];

    public RemoteGgufDownloadOptionRowViewModel? SelectedRemoteDownloadOption
    {
        get => _selectedRemoteDownloadOption;
        set
        {
            if (_selectedRemoteDownloadOption == value)
            {
                return;
            }

            _selectedRemoteDownloadOption = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<WizardStepRowViewModel> WizardSteps { get; } = [];

    public ObservableCollection<string> LaunchReviewLines { get; } = [];

    public ObservableCollection<string> LaunchEnvironmentLines { get; } = [];

    public ObservableCollection<AgentCliStatusRowViewModel> AgentCliStatuses { get; } = [];

    public string LaunchCommandPreview { get; private set; } = "Команда ещё не собрана.";

    public ObservableCollection<DecodingPresetRowViewModel> DecodingPresets { get; }

    public DecodingPresetRowViewModel SelectedDecodingPreset
    {
        get => _selectedDecodingPreset;
        set
        {
            if (_selectedDecodingPreset == value)
            {
                return;
            }

            _selectedDecodingPreset = value;
            OnPropertyChanged();
            RefreshLaunchReview();
        }
    }

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

    public ObservableCollection<PresetRowViewModel> Presets { get; }

    public PresetRowViewModel? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (_selectedPreset == value)
            {
                return;
            }

            _selectedPreset = value;
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<AgentKind> AgentOptions { get; } =
    [
        AgentKind.Kilo,
        AgentKind.OpenCode,
        AgentKind.Claw,
        AgentKind.Aider
    ];

    public AgentKind SelectedAgent
    {
        get => _selectedAgent;
        set
        {
            if (_selectedAgent == value)
            {
                return;
            }

            _selectedAgent = value;
            OnPropertyChanged();
            SetScenario(new LaunchScenario(LaunchMode.Agent, _selectedAgent, _selectedRuntime));
            _lastLaunchPlan = null;
        }
    }

    public IReadOnlyList<RuntimeKind> RuntimeOptions { get; } =
    [
        RuntimeKind.Ollama,
        RuntimeKind.LlamaCpp,
        RuntimeKind.LlamaCppTurboQuant,
        RuntimeKind.LlamaCppMtp
    ];

    public RuntimeKind SelectedRuntime
    {
        get => _selectedRuntime;
        set
        {
            if (_selectedRuntime == value)
            {
                return;
            }

            _selectedRuntime = value;
            OnPropertyChanged();
            var mode = _wizardState.Scenario.Mode;
            SetScenario(new LaunchScenario(mode, mode == LaunchMode.Agent ? _selectedAgent : AgentKind.None, _selectedRuntime));
            _lastLaunchPlan = null;
        }
    }

    [RelayCommand]
    private void SelectAgentMode()
    {
        SetScenario(new LaunchScenario(LaunchMode.Agent, SelectedAgent, SelectedRuntime));
        SetStatus("Режим проекта: далее выбор папки проекта, агента, модели и runtime.");
    }

    [RelayCommand]
    private void SelectEndpointMode()
    {
        SetScenario(new LaunchScenario(LaunchMode.Endpoint, AgentKind.None, SelectedRuntime));
        SetStatus("Режим сервера: далее выбор модели, контекста, KV/MTP и порта.");
    }

    [RelayCommand]
    private void NextStep()
    {
        _wizardState = _wizardState.Next();
        RefreshWizardSteps();
        if (_wizardState.CurrentStep == WizardStep.Review)
        {
            RefreshLaunchReview();
        }

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
    private async Task CheckPortAsync()
    {
        SetStatus($"Проверяю GPU и порт {Port}...");
        var snapshot = await _runtimeDashboardService.CheckAsync(Port, default);
        GpuStatus = snapshot.GpuText;
        PortStatus = snapshot.PortText;
        RuntimeStatus = snapshot.RuntimeText;
        _bestRuntime = snapshot.BestRuntime;
        OnPropertyChanged(nameof(GpuStatus));
        OnPropertyChanged(nameof(PortStatus));
        OnPropertyChanged(nameof(RuntimeStatus));
        SetStatus(snapshot.IsPortFree
            ? "Окружение проверено: порт свободен."
            : "Порт занят. Если это llama-server, его можно будет освободить перед запуском.");
    }

    [RelayCommand]
    private async Task CheckAgentsAsync()
    {
        SetStatus("Проверяю агентные CLI в PATH...");
        var statuses = await _agentCliCatalogService.CheckAsync(default);
        AgentCliStatuses.Clear();
        foreach (var status in statuses)
        {
            AgentCliStatuses.Add(new AgentCliStatusRowViewModel(status));
        }

        var installed = statuses.Count(status => status.IsInstalled);
        SetStatus($"Агенты проверены: {installed}/{statuses.Count} доступны.");
    }

    [RelayCommand]
    private async Task InstallRuntimePackageAsync()
    {
        if (string.IsNullOrWhiteSpace(RuntimeArchivePath))
        {
            SetStatus("Укажите путь к zip-архиву runtime.");
            return;
        }

        var runtimeId = Path.GetFileNameWithoutExtension(RuntimeArchivePath);
        SetStatus($"Устанавливаю runtime: {runtimeId}...");
        try
        {
            var result = await _runtimePackageInstaller.InstallAsync(
                new RuntimePackageInstallRequest(RuntimeArchivePath, RuntimeRootPath, runtimeId),
                default);
            RuntimeStatus = $"runtime: {result.Message}";
            OnPropertyChanged(nameof(RuntimeStatus));
            SetStatus(result.Installed
                ? $"Runtime установлен: {result.Message}"
                : $"Runtime распакован, но не готов: {result.Message}");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetStatus($"Не удалось установить runtime: {ex.Message}");
        }
    }

    [RelayCommand]
    private void RefreshModels()
    {
        RefreshLocalModels();
        SetStatus("Каталог локальных моделей обновлён.");
    }

    [RelayCommand]
    private void ApplySelectedPreset()
    {
        if (SelectedPreset is null)
        {
            SetStatus("Выберите пресет для быстрого запуска.");
            return;
        }

        var profile = SelectedPreset.Profile;
        _selectedAgent = profile.Agent == AgentKind.None ? _selectedAgent : profile.Agent;
        _selectedRuntime = profile.Runtime;
        OnPropertyChanged(nameof(SelectedAgent));
        OnPropertyChanged(nameof(SelectedRuntime));

        if (!string.IsNullOrWhiteSpace(profile.ProjectPath))
        {
            ProjectsFolderPath = profile.ProjectPath;
            OnPropertyChanged(nameof(ProjectsFolderPath));
        }

        Port = profile.Port;
        ContextTokens = profile.ContextTokens;
        var decodingPreset = DecodingPresets.FirstOrDefault(preset => preset.Id == profile.AntiLoopPresetId);
        if (decodingPreset is not null)
        {
            SelectedDecodingPreset = decodingPreset;
        }

        SetScenario(new LaunchScenario(profile.Mode, profile.Mode == LaunchMode.Agent ? profile.Agent : AgentKind.None, profile.Runtime));
        RefreshLaunchReview();
        _lastLaunchPlan = null;
        SetStatus($"Пресет применён: {profile.Name}.");
    }

    [RelayCommand]
    private async Task LoadSettingsAsync()
    {
        if (_settingsStore is null)
        {
            return;
        }

        var settings = await _settingsStore.LoadAsync(default);
        if (settings is null)
        {
            SetStatus("Сохранённые настройки пока не найдены.");
            return;
        }

        ModelsFolderPath = settings.ModelsRoot;
        ProjectsFolderPath = settings.ProjectsRoot ?? "не указана";
        OnPropertyChanged(nameof(ModelsFolderPath));
        OnPropertyChanged(nameof(ProjectsFolderPath));
        RefreshLocalModels();

        if (settings.Profiles.Count > 0)
        {
            Presets.Clear();
            foreach (var profile in settings.Profiles)
            {
                Presets.Add(new PresetRowViewModel(profile));
            }

            SelectedPreset = Presets.FirstOrDefault();
        }

        SetStatus($"Настройки загружены: {settings.Profiles.Count} пресетов.");
    }

    [RelayCommand]
    private async Task SaveCurrentPresetAsync()
    {
        if (_settingsStore is null)
        {
            SetStatus("Хранилище настроек недоступно.");
            return;
        }

        var profile = BuildDraftProfile() with
        {
            Id = $"preset-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            Name = $"Быстрый запуск {Presets.Count + 1}"
        };
        var row = new PresetRowViewModel(profile);
        Presets.Add(row);
        SelectedPreset = row;

        await _settingsStore.SaveAsync(BuildSettingsSnapshot(), default);
        SetStatus($"Пресет сохранён: {profile.Name}.");
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

            SelectedRemoteModel = RemoteModels.FirstOrDefault();
            SetStatus(models.Count == 0
                ? "Hugging Face не вернул GGUF-моделей по этому запросу."
                : $"Hugging Face: найдено {models.Count} моделей.");
        }
        catch (HttpRequestException ex)
        {
            SetStatus($"Hugging Face недоступен: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedRemoteModelAsync()
    {
        if (SelectedRemoteDownloadOption is null)
        {
            SetStatus("Выберите конкретный GGUF-файл для скачивания.");
            return;
        }

        if (ModelsFolderPath == "не указана" || string.IsNullOrWhiteSpace(ModelsFolderPath))
        {
            SetStatus("Сначала укажите папку моделей.");
            return;
        }

        SetStatus($"Скачиваю {SelectedRemoteDownloadOption.Label}...");
        IsDownloading = true;
        _downloadCancellation?.Dispose();
        _downloadCancellation = new CancellationTokenSource();
        try
        {
            var result = await _modelDownloadService.DownloadAsync(
                new HuggingFaceModelDownloadRequest(
                    SelectedRemoteDownloadOption.RepoId,
                    SelectedRemoteDownloadOption.Option,
                    ModelsFolderPath),
                _downloadCancellation.Token,
                UpdateDownloadProgress);

            RefreshLocalModels();
            SetStatus($"Скачивание завершено: {result.DownloadedFiles.Count} скачано, {result.SkippedFiles.Count} уже были на диске.");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Скачивание отменено.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            SetStatus($"Не удалось скачать модель: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
            _downloadCancellation?.Dispose();
            _downloadCancellation = null;
        }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        if (!IsDownloading)
        {
            SetStatus("Активной загрузки нет.");
            return;
        }

        _downloadCancellation?.Cancel();
    }

    private void SetStatus(string message)
    {
        StatusMessage = message;
        OnPropertyChanged(nameof(StatusMessage));
    }

    private void UpdateDownloadProgress(HuggingFaceDownloadProgress progress)
    {
        var percent = progress.TotalBytes is > 0
            ? Math.Clamp(progress.BytesReceived * 100d / progress.TotalBytes.Value, 0, 100)
            : 0;
        DownloadProgressPercent = progress.IsSkipped ? 100 : percent;
        DownloadProgressText = progress.IsSkipped
            ? $"{progress.FileIndex}/{progress.TotalFiles} · уже есть · {progress.FileName}"
            : string.Create(CultureInfo.InvariantCulture, $"{progress.FileIndex}/{progress.TotalFiles} · {progress.FileName} · {DownloadProgressPercent:0}%");
        OnPropertyChanged(nameof(DownloadProgressPercent));
        OnPropertyChanged(nameof(DownloadProgressText));
        SetStatus(progress.IsSkipped
            ? $"Уже есть: {progress.FileName}"
            : $"Скачиваю файл {progress.FileIndex}/{progress.TotalFiles}: {progress.FileName}");
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

    private void RefreshLaunchReview()
    {
        var profile = BuildDraftProfile();

        LaunchReviewLines.Clear();
        foreach (var line in LaunchReviewBuilder.Build(profile).Lines)
        {
            LaunchReviewLines.Add(line);
        }
    }

    [RelayCommand]
    private void BuildLaunchCommand()
    {
        var profile = BuildDraftProfile();
        var plan = profile.Mode == LaunchMode.Endpoint
            ? LlamaServerCommandBuilder.Build(profile, DecodingPresetCatalog.Get(SelectedDecodingPreset.Id))
            : BuildAgentPlan(profile);
        _lastLaunchPlan = plan;
        var preview = LaunchPlanFormatter.Format(plan);

        LaunchCommandPreview = preview.CommandLine;
        OnPropertyChanged(nameof(LaunchCommandPreview));
        LaunchEnvironmentLines.Clear();
        foreach (var line in preview.EnvironmentLines)
        {
            LaunchEnvironmentLines.Add(line);
        }

        foreach (var line in RuntimeCompatibilityService.Check(profile, _bestRuntime).Messages)
        {
            LaunchEnvironmentLines.Add($"RUNTIME: {line}");
        }

        if (profile.Mode == LaunchMode.Agent)
        {
            var status = AgentCliStatuses.FirstOrDefault(row => row.Name == profile.Agent.ToString());
            if (status is { IsInstalled: false })
            {
                LaunchEnvironmentLines.Add($"WARN: агент {profile.Agent} не найден в PATH ({status.Executable}).");
            }
        }

        SetStatus("Команда запуска собрана. Проверьте её перед стартом.");
    }

    [RelayCommand]
    private async Task StartLaunchAsync()
    {
        var profile = BuildDraftProfile();
        var guard = LaunchGuard.Validate(profile);
        if (!guard.CanLaunch)
        {
            SetStatus(string.Join(" ", guard.Messages));
            return;
        }

        var compatibility = RuntimeCompatibilityService.Check(profile, _bestRuntime);
        if (!compatibility.IsCompatible)
        {
            SetStatus($"Запуск остановлен: {string.Join(" ", compatibility.Messages)}");
            return;
        }

        if (_lastLaunchPlan is null)
        {
            BuildLaunchCommand();
        }

        if (_lastLaunchPlan is null)
        {
            SetStatus("Не удалось собрать команду запуска.");
            return;
        }

        var workingDirectory = profile.Mode == LaunchMode.Agent ? profile.ProjectPath : null;
        var result = await _runtimeStartCoordinator.StartAsync(_lastLaunchPlan, profile.Port, workingDirectory, default);
        SetStatus(string.Join(" ", result.Messages));
    }

    private LaunchProfile BuildDraftProfile() => new(
            Id: "draft",
            Name: "Черновик запуска",
            Mode: _wizardState.Scenario.Mode,
            Agent: _wizardState.Scenario.Agent,
            Runtime: _wizardState.Scenario.Runtime,
            ProjectPath: ProjectsFolderPath == "не указана" ? null : ProjectsFolderPath,
            ModelPath: SelectedLocalModel?.Path ?? "модель не выбрана",
            ContextTokens: ContextTokens,
            Port: Port,
            AntiLoopPresetId: SelectedDecodingPreset.Id);

    private static LaunchPlan BuildAgentPlan(LaunchProfile profile)
    {
        var request = new AgentLaunchRequest(
            profile.Agent,
            profile.ProjectPath ?? "",
            "local/llama.cpp/model",
            $"http://127.0.0.1:{profile.Port}/v1");

        IAgentCommandBuilder builder = profile.Agent switch
        {
            AgentKind.OpenCode => new OpenCodeCommandBuilder(),
            AgentKind.Claw => new ClawCommandBuilder(),
            AgentKind.Aider => new AiderCommandBuilder(),
            _ => new KiloCommandBuilder()
        };

        return builder.Build(request);
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

        if (SelectedLocalModel is null && LocalModels.Count > 0)
        {
            SelectedLocalModel = LocalModels[0];
        }
    }

    private void RefreshRemoteDownloadOptions()
    {
        RemoteDownloadOptions.Clear();
        foreach (var option in SelectedRemoteModel?.DownloadOptions ?? [])
        {
            RemoteDownloadOptions.Add(option);
        }

        SelectedRemoteDownloadOption = RemoteDownloadOptions.FirstOrDefault();
    }

    private LauncherSettings BuildSettingsSnapshot() => new(
        ModelsFolderPath == "не указана" ? _defaultModelsDirectory : ModelsFolderPath,
        ProjectsFolderPath == "не указана" ? null : ProjectsFolderPath,
        RuntimeRoot: @"D:\AI\runtimes",
        DownloadsRoot: Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AI Launcher Studio",
            "downloads"),
        DefaultPort: Port,
        Language: "ru",
        HelpMode: "pro",
        Profiles: Presets.Select(preset => preset.Profile).ToArray());

    private static string DefaultSettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AI Launcher Studio",
        "launcher-settings.json");


    private static IEnumerable<PresetRowViewModel> DefaultPresets()
    {
        yield return new PresetRowViewModel(new LaunchProfile(
            "kilo-qwen-turbo",
            "Kilo · Qwen Coder · TurboQuant",
            LaunchMode.Agent,
            AgentKind.Kilo,
            RuntimeKind.LlamaCppTurboQuant,
            null,
            "модель не выбрана",
            65536,
            8080,
            "coding-safe"));
        yield return new PresetRowViewModel(new LaunchProfile(
            "opencode-ollama",
            "OpenCode · Ollama",
            LaunchMode.Agent,
            AgentKind.OpenCode,
            RuntimeKind.Ollama,
            null,
            "модель не выбрана",
            32768,
            11434,
            "coding-safe"));
        yield return new PresetRowViewModel(new LaunchProfile(
            "endpoint-mtp",
            "Сервер · MTP endpoint",
            LaunchMode.Endpoint,
            AgentKind.None,
            RuntimeKind.LlamaCppMtp,
            null,
            "модель не выбрана",
            65536,
            8081,
            "mtp-fast"));
    }
}
