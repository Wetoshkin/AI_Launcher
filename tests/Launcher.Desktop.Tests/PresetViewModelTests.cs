using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;
using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;
using Launcher.Runtimes.Startup;
using Launcher.Runtimes.Status;

namespace Launcher.Desktop.Tests;

public sealed class PresetViewModelTests
{
    [Fact]
    public void PresetRowShowsUsefulRussianSummary()
    {
        var preset = new PresetRowViewModel(new LaunchProfile(
            "kilo-qwen",
            "Kilo + Qwen Coder",
            LaunchMode.Agent,
            AgentKind.Kilo,
            RuntimeKind.LlamaCppTurboQuant,
            @"D:\AI\Projects\App",
            @"D:\AI\Models\qwen.gguf",
            65536,
            8080,
            "coding-safe"));

        Assert.Equal("Kilo + Qwen Coder", preset.Name);
        Assert.Equal("Проект · Kilo · LlamaCppTurboQuant · 64k · порт 8080", preset.Summary);
        Assert.Equal(@"D:\AI\Projects\App", preset.ProjectPath);
    }

    [Fact]
    public void ApplySelectedPresetUpdatesDraftLaunchState()
    {
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            new EmptyDownloadService());
        var preset = new PresetRowViewModel(new LaunchProfile(
            "opencode-gemma",
            "OpenCode + Gemma",
            LaunchMode.Agent,
            AgentKind.OpenCode,
            RuntimeKind.Ollama,
            @"D:\AI\Projects\App",
            @"D:\AI\Models\gemma.gguf",
            32768,
            11434,
            "coding-safe"));

        viewModel.Presets.Clear();
        viewModel.Presets.Add(preset);
        viewModel.SelectedPreset = preset;
        viewModel.ApplySelectedPresetCommand.Execute(null);

        Assert.Equal(AgentKind.OpenCode, viewModel.SelectedAgent);
        Assert.Equal(RuntimeKind.Ollama, viewModel.SelectedRuntime);
        Assert.Equal(@"D:\AI\Projects\App", viewModel.ProjectsFolderPath);
        Assert.Equal(11434, viewModel.Port);
        Assert.Equal(32768, viewModel.ContextTokens);
        Assert.Equal("Пресет применён: OpenCode + Gemma.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task LoadSettingsCommandReplacesDefaultPresetsWithStoredProfiles()
    {
        var store = new MemorySettingsStore(new LauncherSettings(
            ModelsRoot: @"D:\AI\Models",
            ProjectsRoot: @"D:\AI\Projects",
            RuntimeRoot: @"D:\AI\runtimes",
            DownloadsRoot: @"D:\AI\downloads",
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "pro",
            Profiles:
            [
                new LaunchProfile(
                    "saved-kilo",
                    "Сохранённый Kilo",
                    LaunchMode.Agent,
                    AgentKind.Kilo,
                    RuntimeKind.LlamaCppTurboQuant,
                    @"D:\AI\Projects\Saved",
                    @"D:\AI\Models\saved.gguf",
                    65536,
                    8080,
                    "coding-safe")
            ]));
        var viewModel = CreateViewModel(store);

        await viewModel.LoadSettingsCommand.ExecuteAsync(null);

        var preset = Assert.Single(viewModel.Presets);
        Assert.Equal("Сохранённый Kilo", preset.Name);
        Assert.Equal(@"D:\AI\Models", viewModel.ModelsFolderPath);
        Assert.Equal(@"D:\AI\Projects", viewModel.ProjectsFolderPath);
    }

    [Fact]
    public async Task SaveCurrentPresetCommandPersistsDraftProfile()
    {
        var store = new MemorySettingsStore(null);
        var viewModel = CreateViewModel(store);
        viewModel.SelectedAgent = AgentKind.Claw;
        viewModel.SelectedRuntime = RuntimeKind.LlamaCppMtp;
        viewModel.Port = 8081;
        viewModel.ContextTokens = 131072;

        await viewModel.SaveCurrentPresetCommand.ExecuteAsync(null);

        Assert.NotNull(store.Saved);
        Assert.Equal(4, store.Saved.Profiles.Count);
        var profile = store.Saved.Profiles.Last();
        Assert.Equal("Быстрый запуск 4", profile.Name);
        Assert.Equal(AgentKind.Claw, profile.Agent);
        Assert.Equal(RuntimeKind.LlamaCppMtp, profile.Runtime);
        Assert.Equal(8081, profile.Port);
        Assert.Equal(131072, profile.ContextTokens);
        Assert.Equal("Пресет сохранён: Быстрый запуск 4.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task InstallRuntimePackageCommandInstallsArchiveIntoRuntimeRoot()
    {
        var installer = new CapturingRuntimeInstaller(new RuntimePackageInstallResult(
            Installed: true,
            InstallDirectory: @"D:\AI\runtimes\llama-runtime",
            ExecutablePath: @"D:\AI\runtimes\llama-runtime\llama-server.exe",
            Message: "llama-server.exe найден"));
        var viewModel = CreateViewModel(runtimePackageInstaller: installer);
        viewModel.RuntimeArchivePath = @"D:\Downloads\llama-runtime.zip";
        viewModel.RuntimeRootPath = @"D:\AI\runtimes";

        await viewModel.InstallRuntimePackageCommand.ExecuteAsync(null);

        Assert.NotNull(installer.LastRequest);
        Assert.Equal(@"D:\Downloads\llama-runtime.zip", installer.LastRequest.ArchivePath);
        Assert.Equal(@"D:\AI\runtimes", installer.LastRequest.RuntimeRoot);
        Assert.Equal("llama-runtime", installer.LastRequest.RuntimeId);
        Assert.Equal("runtime: llama-server.exe найден", viewModel.RuntimeStatus);
        Assert.Equal("Runtime установлен: llama-server.exe найден", viewModel.StatusMessage);
    }

    [Fact]
    public async Task PickRuntimeArchiveCommandStoresSelectedZipPath()
    {
        var picker = new FixedFilePicker(@"D:\Downloads\llama-runtime.zip");
        var viewModel = CreateViewModel();
        viewModel.FilePicker = picker;

        await viewModel.PickRuntimeArchiveCommand.ExecuteAsync(null);

        Assert.Equal(@"D:\Downloads\llama-runtime.zip", viewModel.RuntimeArchivePath);
        Assert.Equal("Runtime-архив выбран.", viewModel.StatusMessage);
        Assert.Equal(".zip", Assert.Single(picker.LastExtensions));
    }

    [Fact]
    public async Task PickRuntimeArchiveCommandKeepsExistingPathWhenCancelled()
    {
        var picker = new FixedFilePicker(null);
        var viewModel = CreateViewModel();
        viewModel.RuntimeArchivePath = @"D:\Downloads\old-runtime.zip";
        viewModel.FilePicker = picker;

        await viewModel.PickRuntimeArchiveCommand.ExecuteAsync(null);

        Assert.Equal(@"D:\Downloads\old-runtime.zip", viewModel.RuntimeArchivePath);
        Assert.Equal("Выбор runtime-архива отменён.", viewModel.StatusMessage);
    }

    [Fact]
    public void AgentCliStatusRowShowsRussianMissingPath()
    {
        var row = new AgentCliStatusRowViewModel(new Launcher.Agents.Discovery.AgentCliStatus(
            AgentKind.Kilo,
            "kilo",
            IsInstalled: false,
            ExecutablePath: null,
            VersionText: null));

        Assert.Equal("Kilo", row.Name);
        Assert.Equal("kilo", row.Executable);
        Assert.Equal("не найден", row.Status);
        Assert.Equal("не найден в PATH", row.Path);
        Assert.False(row.IsInstalled);
    }

    private static HomeViewModel CreateViewModel(
        ILauncherSettingsStore? settingsStore = null,
        IRuntimePackageInstaller? runtimePackageInstaller = null) => new(
        new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
        new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
        new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
        new EmptyDownloadService(),
        settingsStore,
        agentCliCatalogService: null,
        runtimePackageInstaller: runtimePackageInstaller ?? new EmptyRuntimeInstaller());

    private sealed class EmptyDownloadService : IHuggingFaceModelDownloadService
    {
        public Task<HuggingFaceModelDownloadResult> DownloadAsync(
            HuggingFaceModelDownloadRequest request,
            CancellationToken cancellationToken,
            Action<HuggingFaceDownloadProgress>? progress = null) =>
            Task.FromResult(new HuggingFaceModelDownloadResult([], []));
    }

    private sealed class EmptyRuntimeInstaller : IRuntimePackageInstaller
    {
        public Task<RuntimePackageInstallResult> InstallAsync(RuntimePackageInstallRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new RuntimePackageInstallResult(false, request.RuntimeRoot, null, "не используется"));
    }

    private sealed class CapturingRuntimeInstaller(RuntimePackageInstallResult result) : IRuntimePackageInstaller
    {
        public RuntimePackageInstallRequest? LastRequest { get; private set; }

        public Task<RuntimePackageInstallResult> InstallAsync(RuntimePackageInstallRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class FixedFilePicker(string? path) : Launcher.Desktop.Services.IFilePicker
    {
        public IReadOnlyList<string> LastExtensions { get; private set; } = [];

        public Task<string?> PickFileAsync(string title, IReadOnlyList<string> extensions)
        {
            LastExtensions = extensions;
            return Task.FromResult(path);
        }
    }

    private sealed class MemorySettingsStore(LauncherSettings? initial) : ILauncherSettingsStore
    {
        public LauncherSettings? Saved { get; private set; } = initial;

        public Task<LauncherSettings?> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Saved);

        public Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken)
        {
            Saved = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private sealed class EmptyGpuProbe : IGpuProbe
    {
        public Task<IReadOnlyList<GpuInfo>> GetGpusAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GpuInfo>>([]);
    }

    private sealed class EmptyPortInspector : IPortInspector
    {
        public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken) =>
            Task.FromResult<PortOwnerInfo?>(null);
    }

    private sealed class EmptyPortReleaser : IPortReleaser
    {
        public Task<PortReleaseResult> ReleaseIfSafeAsync(PortOwnerInfo owner, CancellationToken cancellationToken) =>
            Task.FromResult(new PortReleaseResult(Released: false, "не требуется"));
    }

    private sealed class EmptyProcessStarter : IProcessStarter
    {
        public Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new ProcessStartResult(0));
    }
}
