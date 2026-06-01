using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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
using Launcher.Runtimes.Memory;
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
    private readonly IRuntimeReleaseCatalog _runtimeReleaseCatalog;
    private readonly IRuntimeReleaseDownloader _runtimeReleaseDownloader;
    private readonly ILauncherSettingsStore? _settingsStore;
    private readonly IPortInspector _portInspector;
    private readonly IPortReleaser _portReleaser;
    private readonly RuntimeDashboardService _runtimeDashboardService;
    private readonly RuntimeStartCoordinator _runtimeStartCoordinator;
    private readonly IProcessStopper _processStopper;
    private LaunchPlan? _lastLaunchPlan;
    private readonly List<int> _activeProcessIds = [];
    private IReadOnlyList<LocalModelFile> _allModels = [];
    private LaunchWizardState _wizardState;
    private string _modelSearchText = "";
    private string _hfSearchText = "qwen coder gguf";
    private string _runtimeArchivePath = "";
    private string _runtimeRootPath = @"D:\AI\runtimes";
    private string _runtimeCacheRootPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AI Launcher Studio",
        "runtime-downloads");
    private HuggingFaceSort _hfSort = HuggingFaceSort.Downloads;
    private int _mtpDraftTokens = 4;
    private int _port = 8080;
    private int _contextTokens = 65536;
    private DecodingPresetRowViewModel _selectedDecodingPreset;
    private ModelRowViewModel? _selectedLocalModel;
    private PresetRowViewModel? _selectedPreset;
    private RemoteModelRowViewModel? _selectedRemoteModel;
    private RemoteGgufDownloadOptionRowViewModel? _selectedRemoteDownloadOption;
    private bool _isDownloading;
    private bool _isRuntimeDownloading;
    private CancellationTokenSource? _downloadCancellation;
    private CancellationTokenSource? _runtimeDownloadCancellation;
    private LlamaRuntimeInfo? _bestRuntime;
    private double? _lastGpuUsedGb;
    private double? _lastGpuTotalGb;
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
        new RuntimePackageInstaller(),
        new RuntimeReleaseCatalogService(new GitHubReleaseClient(new HttpClient())),
        new RuntimeReleaseDownloadService(new HttpClient()))
    {
    }

    public HomeViewModel(
        HuggingFaceModelClient huggingFaceClient,
        RuntimeDashboardService runtimeDashboardService,
        RuntimeStartCoordinator runtimeStartCoordinator,
        IHuggingFaceModelDownloadService modelDownloadService,
        ILauncherSettingsStore? settingsStore = null,
        AgentCliCatalogService? agentCliCatalogService = null,
        IRuntimePackageInstaller? runtimePackageInstaller = null,
        IRuntimeReleaseCatalog? runtimeReleaseCatalog = null,
        IRuntimeReleaseDownloader? runtimeReleaseDownloader = null,
        IPortInspector? portInspector = null,
        IPortReleaser? portReleaser = null,
        IProcessStopper? processStopper = null)
    {
        _huggingFaceClient = huggingFaceClient;
        _modelDownloadService = modelDownloadService;
        _agentCliCatalogService = agentCliCatalogService ?? new AgentCliCatalogService(new WindowsExecutableResolver());
        _runtimePackageInstaller = runtimePackageInstaller ?? new RuntimePackageInstaller();
        _runtimeReleaseCatalog = runtimeReleaseCatalog ?? new RuntimeReleaseCatalogService(new GitHubReleaseClient(new HttpClient()));
        _runtimeReleaseDownloader = runtimeReleaseDownloader ?? new RuntimeReleaseDownloadService(new HttpClient());
        _settingsStore = settingsStore;
        _portInspector = portInspector ?? new WindowsPortInspector();
        _portReleaser = portReleaser ?? new PortReleaseService(new ProcessCommandRunner());
        _runtimeDashboardService = runtimeDashboardService;
        _runtimeStartCoordinator = runtimeStartCoordinator;
        _processStopper = processStopper ?? new ProcessStopper();
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

    public string ActiveProcessStatus { get; private set; } = "процесс: не запущен";

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

    public string RuntimeCacheRootPath
    {
        get => _runtimeCacheRootPath;
        set
        {
            if (_runtimeCacheRootPath == value)
            {
                return;
            }

            _runtimeCacheRootPath = value;
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

    public int MtpDraftTokens
    {
        get => _mtpDraftTokens;
        set
        {
            var normalized = Math.Clamp(value, 1, 16);
            if (_mtpDraftTokens == normalized)
            {
                return;
            }

            _mtpDraftTokens = normalized;
            OnPropertyChanged();
            RefreshLaunchReview();
            _lastLaunchPlan = null;
        }
    }

    public string DownloadProgressText { get; private set; } = "Загрузок нет.";

    public double DownloadProgressPercent { get; private set; }

    public string RuntimeDownloadProgressText { get; private set; } = "Runtime загрузок нет.";

    public double RuntimeDownloadProgressPercent { get; private set; }

    public string RuntimeUpdateStatus { get; private set; } = "Обновление runtime: не проверялось.";

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

    public bool IsRuntimeDownloading
    {
        get => _isRuntimeDownloading;
        private set
        {
            if (_isRuntimeDownloading == value)
            {
                return;
            }

            _isRuntimeDownloading = value;
            OnPropertyChanged();
        }
    }

    public IFolderPicker? FolderPicker { get; set; }

    public IFilePicker? FilePicker { get; set; }

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

    public ObservableCollection<string> ProcessLogLines { get; } = [];

    public ObservableCollection<AgentCliStatusRowViewModel> AgentCliStatuses { get; } = [];

    public ObservableCollection<RuntimeReleasePackageRowViewModel> RuntimeReleasePackages { get; } = [];

    public string LaunchCommandPreview { get; private set; } = "Команда ещё не собрана.";

    private RuntimeReleasePackageRowViewModel? _selectedRuntimeReleasePackage;
    private RuntimeReleaseProfile _selectedRuntimeReleaseProfile = RuntimeReleaseProfile.Cuda;

    public IReadOnlyList<RuntimeReleaseProfile> RuntimeReleaseProfileOptions { get; } =
    [
        RuntimeReleaseProfile.Cpu,
        RuntimeReleaseProfile.Cuda,
        RuntimeReleaseProfile.Vulkan,
        RuntimeReleaseProfile.Rocm
    ];

    public RuntimeReleaseProfile SelectedRuntimeReleaseProfile
    {
        get => _selectedRuntimeReleaseProfile;
        set
        {
            if (_selectedRuntimeReleaseProfile == value)
            {
                return;
            }

            _selectedRuntimeReleaseProfile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RuntimeReleaseProfileHint));
        }
    }

    public string RuntimeReleaseProfileHint => SelectedRuntimeReleaseProfile switch
    {
        RuntimeReleaseProfile.Cuda => "CUDA: NVIDIA GPU, обычно самый быстрый вариант для RTX.",
        RuntimeReleaseProfile.Vulkan => "Vulkan: универсальный GPU runtime, часто подходит для NVIDIA/AMD/Intel.",
        RuntimeReleaseProfile.Rocm => "ROCm: AMD GPU runtime для совместимых Radeon/Instinct.",
        _ => "CPU: запуск без GPU-ускорения, самый совместимый вариант."
    };

    public RuntimeReleasePackageRowViewModel? SelectedRuntimeReleasePackage
    {
        get => _selectedRuntimeReleasePackage;
        set
        {
            if (_selectedRuntimeReleasePackage == value)
            {
                return;
            }

            _selectedRuntimeReleasePackage = value;
            OnPropertyChanged();
        }
    }

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
        _lastGpuUsedGb = snapshot.UsedGpuGb;
        _lastGpuTotalGb = snapshot.TotalGpuGb;
        OnPropertyChanged(nameof(GpuStatus));
        OnPropertyChanged(nameof(PortStatus));
        OnPropertyChanged(nameof(RuntimeStatus));
        RefreshLaunchReview();
        SetStatus(snapshot.IsPortFree
            ? "Окружение проверено: порт свободен."
            : "Порт занят. Если это llama-server, его можно будет освободить перед запуском.");
    }

    [RelayCommand]
    private async Task ReleasePortAsync()
    {
        SetStatus($"Проверяю, можно ли освободить порт {Port}...");
        var owner = await _portInspector.InspectAsync(Port, default);
        if (owner is null)
        {
            PortStatus = $"порт {Port}: свободен";
            OnPropertyChanged(nameof(PortStatus));
            SetStatus($"Порт {Port} уже свободен.");
            return;
        }

        var result = await _portReleaser.ReleaseIfSafeAsync(owner, default);
        PortStatus = result.Released
            ? $"порт {Port}: освобождён"
            : $"порт {Port}: занят";
        OnPropertyChanged(nameof(PortStatus));
        SetStatus(result.Message);
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
    private async Task ChooseRuntimeRootFolderAsync()
    {
        if (FolderPicker is null)
        {
            SetStatus("Выбор папки установки runtime пока недоступен: окно ещё не готово.");
            return;
        }

        var folder = await FolderPicker.PickFolderAsync("Выберите папку установки runtime");
        if (string.IsNullOrWhiteSpace(folder))
        {
            SetStatus("Выбор папки установки runtime отменён.");
            return;
        }

        RuntimeRootPath = folder;
        SetStatus("Папка установки runtime обновлена.");
    }

    [RelayCommand]
    private async Task ChooseRuntimeCacheFolderAsync()
    {
        if (FolderPicker is null)
        {
            SetStatus("Выбор папки кэша runtime пока недоступен: окно ещё не готово.");
            return;
        }

        var folder = await FolderPicker.PickFolderAsync("Выберите папку кэша runtime");
        if (string.IsNullOrWhiteSpace(folder))
        {
            SetStatus("Выбор папки кэша runtime отменён.");
            return;
        }

        RuntimeCacheRootPath = folder;
        SetStatus("Папка кэша runtime обновлена.");
    }

    [RelayCommand]
    private async Task SearchRuntimeReleasesAsync()
    {
        SetStatus("Ищу runtime-пакеты llama.cpp...");
        try
        {
            var packages = await _runtimeReleaseCatalog.ListPackagesAsync(SelectedRuntimeReleaseProfile, default);
            RuntimeReleasePackages.Clear();
            foreach (var package in packages.Take(12))
            {
                RuntimeReleasePackages.Add(new RuntimeReleasePackageRowViewModel(package));
            }

            SelectedRuntimeReleasePackage = RuntimeReleasePackages.FirstOrDefault();
            SetStatus($"Найдено runtime-пакетов: {RuntimeReleasePackages.Count}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            SetStatus($"Не удалось получить runtime-релизы: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CheckRuntimeUpdateAsync()
    {
        SetStatus("Проверяю обновление runtime...");
        try
        {
            var packages = await _runtimeReleaseCatalog.ListPackagesAsync(SelectedRuntimeReleaseProfile, default);
            var result = RuntimeUpdateService.Check(RuntimeArchivePath, packages);
            RuntimeUpdateStatus = result.Message;
            OnPropertyChanged(nameof(RuntimeUpdateStatus));
            SetStatus($"Проверка обновления runtime: {result.Message}");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException)
        {
            RuntimeUpdateStatus = $"ошибка проверки: {ex.Message}";
            OnPropertyChanged(nameof(RuntimeUpdateStatus));
            SetStatus($"Не удалось проверить обновление runtime: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DownloadSelectedRuntimeReleaseAsync()
    {
        _ = await DownloadSelectedRuntimeReleaseArchiveAsync();
    }

    [RelayCommand]
    private async Task DownloadAndInstallSelectedRuntimeReleaseAsync()
    {
        var download = await DownloadSelectedRuntimeReleaseArchiveAsync();
        if (download is null)
        {
            return;
        }

        var runtimeId = Path.GetFileNameWithoutExtension(download.ArchivePath);
        try
        {
            var result = await _runtimePackageInstaller.InstallAsync(
                new RuntimePackageInstallRequest(download.ArchivePath, RuntimeRootPath, runtimeId),
                default);
            RuntimeStatus = $"runtime: {result.Message}";
            OnPropertyChanged(nameof(RuntimeStatus));
            SetStatus(result.Installed
                ? $"Runtime скачан и установлен: {result.Message}"
                : $"Runtime скачан, но не готов: {result.Message}");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetStatus($"Runtime скачан, но установка не удалась: {ex.Message}");
        }
    }

    private async Task<RuntimeReleaseDownloadResult?> DownloadSelectedRuntimeReleaseArchiveAsync()
    {
        if (SelectedRuntimeReleasePackage is null)
        {
            SetStatus("Выберите runtime-пакет для скачивания.");
            return null;
        }

        SetStatus($"Скачиваю runtime: {SelectedRuntimeReleasePackage.Package.AssetName}...");
        IsRuntimeDownloading = true;
        _runtimeDownloadCancellation?.Dispose();
        _runtimeDownloadCancellation = new CancellationTokenSource();
        try
        {
            var result = await _runtimeReleaseDownloader.DownloadAsync(
                new RuntimeReleaseDownloadRequest(SelectedRuntimeReleasePackage.Package, RuntimeCacheRootPath),
                _runtimeDownloadCancellation.Token,
                UpdateRuntimeDownloadProgress);
            RuntimeArchivePath = result.ArchivePath;
            SetStatus($"Runtime скачан: {result.Message}");
            return result;
        }
        catch (OperationCanceledException)
        {
            SetStatus("Скачивание runtime отменено.");
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetStatus($"Не удалось скачать runtime: {ex.Message}");
            return null;
        }
        finally
        {
            IsRuntimeDownloading = false;
            _runtimeDownloadCancellation?.Dispose();
            _runtimeDownloadCancellation = null;
        }
    }

    [RelayCommand]
    private void CancelRuntimeDownload()
    {
        if (!IsRuntimeDownloading)
        {
            SetStatus("Активной загрузки runtime нет.");
            return;
        }

        _runtimeDownloadCancellation?.Cancel();
    }

    [RelayCommand]
    private async Task PickRuntimeArchiveAsync()
    {
        if (FilePicker is null)
        {
            SetStatus("Выбор runtime-архива пока недоступен: окно ещё не готово.");
            return;
        }

        var file = await FilePicker.PickFileAsync("Выберите zip-архив runtime", [".zip"]);
        if (string.IsNullOrWhiteSpace(file))
        {
            SetStatus("Выбор runtime-архива отменён.");
            return;
        }

        RuntimeArchivePath = file;
        SetStatus("Runtime-архив выбран.");
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
        RuntimeRootPath = settings.RuntimeRoot;
        RuntimeCacheRootPath = settings.DownloadsRoot;
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

    private void UpdateRuntimeDownloadProgress(RuntimeReleaseDownloadProgress progress)
    {
        var percent = progress.TotalBytes is > 0
            ? Math.Clamp(progress.BytesReceived * 100d / progress.TotalBytes.Value, 0, 100)
            : 0;
        RuntimeDownloadProgressPercent = progress.IsSkipped ? 100 : percent;
        RuntimeDownloadProgressText = progress.IsSkipped
            ? $"Runtime: уже есть · {progress.AssetName}"
            : string.Create(CultureInfo.InvariantCulture, $"Runtime: {progress.AssetName} · {RuntimeDownloadProgressPercent:0}%");
        OnPropertyChanged(nameof(RuntimeDownloadProgressPercent));
        OnPropertyChanged(nameof(RuntimeDownloadProgressText));
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

        if (SelectedLocalModel is not null)
        {
            LaunchReviewLines.Add(BuildMemoryForecastLine(profile, SelectedLocalModel.Model));
        }
    }

    [RelayCommand]
    private void BuildLaunchCommand()
    {
        var profile = BuildDraftProfile();
        var plan = profile.Mode == LaunchMode.Endpoint
            ? BuildServerPlan(profile)
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

        ProcessLogLines.Clear();
        _activeProcessIds.Clear();
        RuntimeStartResult result;
        try
        {
            result = profile.Mode == LaunchMode.Agent
                ? await StartAgentScenarioAsync(profile)
                : await StartSinglePlanAsync(_lastLaunchPlan, profile.Port, workingDirectory: null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            ActiveProcessStatus = "процесс: ошибка запуска";
            OnPropertyChanged(nameof(ActiveProcessStatus));
            SetStatus($"Запуск не удался: {ex.Message}");
            return;
        }

        RefreshActiveProcessStatus();
        SetStatus(string.Join(" ", result.Messages));
    }

    private async Task<RuntimeStartResult> StartAgentScenarioAsync(LaunchProfile profile)
    {
        var messages = new List<string>();
        var serverProfile = profile with { Mode = LaunchMode.Endpoint, Agent = AgentKind.None };
        var serverResult = await StartSinglePlanAsync(BuildServerPlan(serverProfile), profile.Port, workingDirectory: null);
        messages.AddRange(serverResult.Messages);
        if (!serverResult.Started)
        {
            return new RuntimeStartResult(false, null, messages);
        }

        var agentResult = await StartSinglePlanAsync(BuildAgentPlan(profile), profile.Port, profile.ProjectPath);
        messages.AddRange(agentResult.Messages);
        return new RuntimeStartResult(agentResult.Started, agentResult.ProcessId, messages);
    }

    private async Task<RuntimeStartResult> StartSinglePlanAsync(
        LaunchPlan plan,
        int port,
        string? workingDirectory)
    {
        var result = await _runtimeStartCoordinator.StartAsync(
            plan,
            port,
            workingDirectory,
            default,
            AppendProcessLogLine);
        if (result.ProcessId is not null)
        {
            _activeProcessIds.Add(result.ProcessId.Value);
        }

        return result;
    }

    private void RefreshActiveProcessStatus()
    {
        ActiveProcessStatus = _activeProcessIds.Count == 0
            ? "процесс: запуск завершён"
            : $"процесс: запущен, PID {string.Join(", ", _activeProcessIds)}";
        OnPropertyChanged(nameof(ActiveProcessStatus));
    }

    private void AppendProcessLogLine(string line)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AppendProcessLogLine(line));
            return;
        }

        if (ProcessLogLines.Count >= 300)
        {
            ProcessLogLines.RemoveAt(0);
        }

        ProcessLogLines.Add(line);
    }

    [RelayCommand]
    private async Task StopLaunchAsync()
    {
        if (_activeProcessIds.Count == 0)
        {
            SetStatus("Активный процесс не запущен.");
            return;
        }

        var messages = new List<string>();
        foreach (var processId in _activeProcessIds.AsEnumerable().Reverse().ToArray())
        {
            var result = await _processStopper.StopAsync(processId, default);
            messages.Add(result.Message);
        }

        _activeProcessIds.Clear();
        ActiveProcessStatus = "процесс: остановлен";
        OnPropertyChanged(nameof(ActiveProcessStatus));
        SetStatus(string.Join(" ", messages));
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

    private LaunchPlan BuildServerPlan(LaunchProfile profile)
    {
        var plan = LlamaServerCommandBuilder.Build(profile, BuildSelectedDecodingPreset());
        return _bestRuntime is null
            ? plan
            : plan with { Executable = _bestRuntime.ExecutablePath };
    }

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

    private DecodingPreset BuildSelectedDecodingPreset()
    {
        var preset = DecodingPresetCatalog.Get(SelectedDecodingPreset.Id);
        if (!preset.EnableMtp)
        {
            return preset;
        }

        var arguments = new Dictionary<string, string>(preset.Arguments, StringComparer.OrdinalIgnoreCase)
        {
            ["--spec-draft-n-max"] = MtpDraftTokens.ToString(CultureInfo.InvariantCulture)
        };
        return preset with { Arguments = arguments };
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
        RuntimeRoot: RuntimeRootPath,
        DownloadsRoot: RuntimeCacheRootPath,
        DefaultPort: Port,
        Language: "ru",
        HelpMode: "pro",
        Profiles: Presets.Select(preset => preset.Profile).ToArray());

    private static string DefaultSettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AI Launcher Studio",
        "launcher-settings.json");

    private string BuildMemoryForecastLine(LaunchProfile profile, LocalModelFile model)
    {
        var estimate = MemoryEstimator.Estimate(
            new ModelMemorySpec(model.SizeGb, ParametersFromSizeLabel(model), NativeContextTokens: null),
            profile.ContextTokens,
            KvCacheFor(profile.Runtime));
        var budgetText = _lastGpuTotalGb is > 0 && _lastGpuUsedGb is { } usedGpuGb
            ? GpuBudgetText(estimate.TotalGb, Math.Max(0.0, _lastGpuTotalGb.Value - usedGpuGb))
            : "GPU ещё не проверен";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Память: веса {estimate.WeightsGb:0.0} ГБ + KV {estimate.KvCacheGb:0.0} ГБ + запас {estimate.OverheadGb:0.0} ГБ = {estimate.TotalGb:0.0} ГБ; {budgetText}.");
    }

    private static string GpuBudgetText(double requiredGb, double freeGpuGb)
    {
        var fitText = requiredGb <= freeGpuGb * 0.92
            ? "поместится"
            : "может не поместиться";
        return string.Create(CultureInfo.InvariantCulture, $"GPU свободно {freeGpuGb:0.0} ГБ; {fitText}");
    }

    private static KvCacheProfile KvCacheFor(RuntimeKind runtime) => runtime == RuntimeKind.LlamaCppTurboQuant
        ? new KvCacheProfile("q8_0", "turbo4")
        : KvCacheProfile.Symmetric("q8_0");

    private static double ParametersFromSizeLabel(LocalModelFile model)
    {
        if (model.SizeLabel is { Length: > 1 } label
            && label.EndsWith("B", StringComparison.OrdinalIgnoreCase)
            && double.TryParse(label[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return Math.Max(1.0, model.SizeGb * 2.0);
    }

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
