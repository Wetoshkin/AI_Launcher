using Launcher.Core.LaunchPlans;
using Launcher.Core.Scenarios;
using Launcher.Agents.Discovery;
using Launcher.Desktop.Services;
using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;
using Launcher.Runtimes.Startup;
using Launcher.Runtimes.Status;

namespace Launcher.Desktop.Tests;

public sealed class RuntimeCompatibilityFlowTests
{
    [Fact]
    public async Task BuildLaunchCommandShowsRuntimeCompatibilityWarnings()
    {
        using var temp = new TempDirectory();
        var viewModel = CreateViewModel(Runtime(supportsMtp: false, supportsTurboQuant: true), modelsDirectory: temp.Path);
        viewModel.SelectedRuntime = RuntimeKind.LlamaCppMtp;
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);

        await viewModel.CheckPortCommand.ExecuteAsync(null);
        viewModel.BuildLaunchCommandCommand.Execute(null);

        Assert.Contains(
            "RUNTIME: Выбран MTP, но runtime не поддерживает --spec-type draft-mtp.",
            viewModel.LaunchEnvironmentLines);
    }

    [Fact]
    public async Task BuildLaunchCommandUsesDetectedRuntimeExecutablePath()
    {
        using var temp = new TempDirectory();
        var viewModel = CreateViewModel(Runtime(supportsMtp: false, supportsTurboQuant: true), modelsDirectory: temp.Path);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);

        await viewModel.CheckPortCommand.ExecuteAsync(null);
        viewModel.BuildLaunchCommandCommand.Execute(null);

        Assert.Contains(@"D:\AI\runtimes\llama-server.exe", viewModel.LaunchCommandPreview);
    }

    [Fact]
    public async Task StartLaunchBlocksIncompatibleRuntimeBeforeProcessStart()
    {
        using var temp = new TempDirectory();
        var starter = new CountingProcessStarter();
        var viewModel = CreateViewModel(Runtime(supportsMtp: false, supportsTurboQuant: true), starter, temp.Path);
        viewModel.SelectedRuntime = RuntimeKind.LlamaCppMtp;
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);

        await viewModel.CheckPortCommand.ExecuteAsync(null);
        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Equal(0, starter.StartCount);
        Assert.Equal("Запуск остановлен: Выбран MTP, но runtime не поддерживает --spec-type draft-mtp.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task StopLaunchCommandStopsStartedProcessAndClearsStatus()
    {
        using var temp = new TempDirectory();
        var stopper = new RecordingProcessStopper();
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            new CountingProcessStarter(processId: 3210),
            temp.Path,
            stopper);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);
        await viewModel.StopLaunchCommand.ExecuteAsync(null);

        Assert.Equal(3210, stopper.StoppedProcessId);
        Assert.Equal("процесс: остановлен", viewModel.ActiveProcessStatus);
        Assert.Equal("Процесс 3210 остановлен.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task StartLaunchAppendsProcessOutputToLog()
    {
        using var temp = new TempDirectory();
        var starter = new CountingProcessStarter(processId: 3210, outputLine: "llama server listening");
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            temp.Path);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Contains("llama server listening", viewModel.ProcessLogLines);
    }

    [Fact]
    public async Task EndpointLaunchStartsFakeServerAndReportsReadyHealth()
    {
        using var temp = new TempDirectory();
        var starter = new CountingProcessStarter(processId: 4242, outputLine: "llama server listening");
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            temp.Path,
            healthClient: new FixedEndpointHealthClient(new EndpointHealthResult(true, 2, "endpoint готов")));
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Single(starter.Requests);
        Assert.Equal(@"D:\AI\runtimes\llama-server.exe", starter.Requests[0].Executable);
        Assert.Equal("процесс: запущен, PID 4242", viewModel.ActiveProcessStatus);
        Assert.Contains("endpoint готов", viewModel.StatusMessage);
        Assert.Contains("llama server listening", viewModel.ProcessLogLines);
    }

    [Fact]
    public async Task StartLaunchBlocksMissingLocalModelBeforeProcessStart()
    {
        using var temp = new EmptyTempDirectory();
        var starter = new CountingProcessStarter();
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            temp.Path);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Empty(starter.Requests);
        Assert.Equal("Выберите модель перед запуском.", viewModel.StatusMessage);
        Assert.Equal("процесс: не запущен", viewModel.ActiveProcessStatus);
    }

    [Fact]
    public async Task StartLaunchBlocksMissingRuntimeBeforeProcessStart()
    {
        using var temp = new TempDirectory();
        var starter = new CountingProcessStarter();
        var viewModel = CreateViewModel(
            [],
            starter,
            temp.Path);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);

        await viewModel.CheckPortCommand.ExecuteAsync(null);
        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Empty(starter.Requests);
        Assert.Equal("Запуск остановлен: Runtime llama-server не проверен.", viewModel.StatusMessage);
        Assert.Equal("процесс: не запущен", viewModel.ActiveProcessStatus);
    }

    [Fact]
    public async Task StartLaunchBlocksMissingAgentCliBeforeProcessStart()
    {
        using var temp = new TempDirectory();
        var starter = new CountingProcessStarter();
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            temp.Path,
            agentCliCatalogService: new AgentCliCatalogService(new MissingExecutableResolver()));
        viewModel.SelectAgentModeCommand.Execute(null);
        viewModel.FolderPicker = new FixedFolderPicker(temp.Path);
        await viewModel.ChooseProjectsFolderCommand.ExecuteAsync(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckAgentsCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Empty(starter.Requests);
        Assert.Equal("Запуск остановлен: агент Kilo не найден в PATH (kilo).", viewModel.StatusMessage);
        Assert.Equal("процесс: не запущен", viewModel.ActiveProcessStatus);
    }

    [Fact]
    public async Task AgentLaunchStartsServerBeforeAgentCli()
    {
        using var temp = new TempDirectory();
        var starter = new CountingProcessStarter(processIds: [3100, 3200]);
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            temp.Path);
        viewModel.SelectAgentModeCommand.Execute(null);
        viewModel.FolderPicker = new FixedFolderPicker(temp.Path);
        await viewModel.ChooseProjectsFolderCommand.ExecuteAsync(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Equal(2, starter.Requests.Count);
        Assert.Equal(@"D:\AI\runtimes\llama-server.exe", starter.Requests[0].Executable);
        Assert.Equal("kilo", starter.Requests[1].Executable);
    }

    [Fact]
    public async Task AgentLaunchWritesProjectConfigBeforeStartingCli()
    {
        using var temp = new TempDirectory();
        var starter = new CountingProcessStarter(processIds: [3100, 3200]);
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            temp.Path);
        viewModel.SelectAgentModeCommand.Execute(null);
        viewModel.FolderPicker = new FixedFolderPicker(temp.Path);
        await viewModel.ChooseProjectsFolderCommand.ExecuteAsync(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        var configPath = Path.Combine(temp.Path, "kilo.jsonc");
        Assert.True(File.Exists(configPath));
        Assert.Contains("kilo.jsonc обновлён.", viewModel.ProcessLogLines);
    }

    [Fact]
    public async Task AgentLaunchFlowBuildsPreviewStartsFakeEndpointThenAgentAndReportsReadyHealth()
    {
        using var models = new TempDirectory();
        using var project = new EmptyTempDirectory();
        var starter = new CountingProcessStarter(processIds: [3100, 3200], outputLine: "fake process started");
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            models.Path,
            healthClient: new FixedEndpointHealthClient(new EndpointHealthResult(true, 1, "endpoint готов")));
        viewModel.SelectAgentModeCommand.Execute(null);
        viewModel.FolderPicker = new FixedFolderPicker(project.Path);
        await viewModel.ChooseProjectsFolderCommand.ExecuteAsync(null);
        viewModel.FolderPicker = new FixedFolderPicker(models.Path);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        viewModel.BuildLaunchCommandCommand.Execute(null);
        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        var modelPath = Path.Combine(models.Path, "Qwen3-Coder-Q4_K_M.gguf");
        Assert.Contains("SERVER:", viewModel.LaunchCommandPreview);
        Assert.Contains("AGENT:", viewModel.LaunchCommandPreview);
        Assert.Contains(modelPath, viewModel.LaunchCommandPreview);
        Assert.Equal(2, starter.Requests.Count);
        Assert.Equal(@"D:\AI\runtimes\llama-server.exe", starter.Requests[0].Executable);
        Assert.Contains(modelPath, starter.Requests[0].Arguments);
        Assert.Null(starter.Requests[0].WorkingDirectory);
        Assert.Equal("kilo", starter.Requests[1].Executable);
        Assert.Equal(project.Path, starter.Requests[1].WorkingDirectory);
        Assert.Equal("процесс: запущен, PID 3100, 3200", viewModel.ActiveProcessStatus);
        Assert.Contains("endpoint готов", viewModel.StatusMessage);
        Assert.Contains("fake process started", viewModel.ProcessLogLines);
        Assert.True(File.Exists(Path.Combine(project.Path, "kilo.jsonc")));
    }

    [Fact]
    public async Task StartLaunchReportsProcessStartFailureWithoutThrowing()
    {
        using var temp = new TempDirectory();
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            new ThrowingProcessStarter("kilo не найден"),
            temp.Path);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Equal("Запуск не удался: kilo не найден", viewModel.StatusMessage);
        Assert.Equal("процесс: ошибка запуска", viewModel.ActiveProcessStatus);
    }

    [Fact]
    public async Task StartLaunchKeepsProcessControllableWhenEndpointHealthFails()
    {
        using var temp = new TempDirectory();
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            new CountingProcessStarter(processId: 777),
            temp.Path,
            healthClient: new FixedEndpointHealthClient(new EndpointHealthResult(false, 30, "endpoint не ответил")));
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Equal("процесс: запущен, PID 777", viewModel.ActiveProcessStatus);
        Assert.Contains("endpoint не ответил", viewModel.StatusMessage);
    }

    [Fact]
    public async Task StartLaunchReportsBusyUnknownPortWithoutReleasingOrStartingProcess()
    {
        using var temp = new TempDirectory();
        var busyPortInspector = new FixedPortInspector(new PortOwnerInfo(
            Port: 8080,
            ProcessId: 4321,
            ProcessName: "postgres",
            ExecutablePath: @"C:\PostgreSQL\postgres.exe",
            EndpointResponds: false,
            LoadedModelId: null));
        var releaser = new RecordingPortReleaser();
        var starter = new CountingProcessStarter();
        var viewModel = CreateViewModel(
            Runtime(supportsMtp: false, supportsTurboQuant: true),
            starter,
            temp.Path,
            startPortInspector: busyPortInspector,
            startPortReleaser: releaser);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        await viewModel.CheckPortCommand.ExecuteAsync(null);

        await viewModel.StartLaunchCommand.ExecuteAsync(null);

        Assert.Equal(0, releaser.ReleaseCount);
        Assert.Equal(0, starter.StartCount);
        Assert.Equal("порт 8080: занят postgres", viewModel.PortStatus);
        Assert.Equal("Порт 8080 занят процессом postgres. Запуск остановлен.", viewModel.StatusMessage);
    }

    private static HomeViewModel CreateViewModel(
        LlamaRuntimeInfo runtime,
        IProcessStarter? starter = null,
        string? modelsDirectory = null,
        IProcessStopper? stopper = null,
        IEndpointHealthClient? healthClient = null,
        IPortInspector? startPortInspector = null,
        IPortReleaser? startPortReleaser = null,
        AgentCliCatalogService? agentCliCatalogService = null) =>
        CreateViewModel(
            [runtime],
            starter,
            modelsDirectory,
            stopper,
            healthClient,
            startPortInspector,
            startPortReleaser,
            agentCliCatalogService);

    private static HomeViewModel CreateViewModel(
        IReadOnlyList<LlamaRuntimeInfo> runtimes,
        IProcessStarter? starter = null,
        string? modelsDirectory = null,
        IProcessStopper? stopper = null,
        IEndpointHealthClient? healthClient = null,
        IPortInspector? startPortInspector = null,
        IPortReleaser? startPortReleaser = null,
        AgentCliCatalogService? agentCliCatalogService = null)
    {
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(
                new EmptyGpuProbe(),
                new EmptyPortInspector(),
                new FakeRuntimeCatalog(runtimes)),
            new RuntimeStartCoordinator(
                startPortInspector ?? new EmptyPortInspector(),
                startPortReleaser ?? new EmptyPortReleaser(),
                starter ?? new CountingProcessStarter(),
                healthClient),
            new EmptyDownloadService(),
            agentCliCatalogService: agentCliCatalogService,
            processStopper: stopper);
        if (modelsDirectory is not null)
        {
            viewModel.FolderPicker = new FixedFolderPicker(modelsDirectory);
        }

        return viewModel;
    }

    private static LlamaRuntimeInfo Runtime(bool supportsMtp, bool supportsTurboQuant) => new(
        @"D:\AI\runtimes\llama-server.exe",
        new LlamaServerCapabilities(
            new HashSet<string>(),
            new HashSet<string>(),
            new HashSet<string>(),
            supportsTurboQuant,
            supportsMtp));

    private sealed class FakeRuntimeCatalog(IReadOnlyList<LlamaRuntimeInfo> runtimes) : ILlamaRuntimeCatalog
    {
        public Task<IReadOnlyList<LlamaRuntimeInfo>> ScanAsync(IEnumerable<string> runtimeRoots, CancellationToken cancellationToken) =>
            Task.FromResult(runtimes);
    }

    private sealed class EmptyDownloadService : IHuggingFaceModelDownloadService
    {
        public Task<HuggingFaceModelDownloadResult> DownloadAsync(
            HuggingFaceModelDownloadRequest request,
            CancellationToken cancellationToken,
            Action<HuggingFaceDownloadProgress>? progress = null) =>
            Task.FromResult(new HuggingFaceModelDownloadResult([], []));
    }

    private sealed class FixedFolderPicker(string path) : IFolderPicker
    {
        public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(path);
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

    private sealed class FixedPortInspector(PortOwnerInfo? owner) : IPortInspector
    {
        public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken) =>
            Task.FromResult(owner);
    }

    private sealed class EmptyPortReleaser : IPortReleaser
    {
        public Task<PortReleaseResult> ReleaseIfSafeAsync(PortOwnerInfo owner, CancellationToken cancellationToken) =>
            Task.FromResult(new PortReleaseResult(Released: false, "не требуется"));
    }

    private sealed class RecordingPortReleaser : IPortReleaser
    {
        public int ReleaseCount { get; private set; }

        public Task<PortReleaseResult> ReleaseIfSafeAsync(PortOwnerInfo owner, CancellationToken cancellationToken)
        {
            ReleaseCount++;
            return Task.FromResult(new PortReleaseResult(Released: true, "порт освобождён"));
        }
    }

    private sealed class CountingProcessStarter(int processId = 123, string? outputLine = null, IReadOnlyList<int>? processIds = null) : IProcessStarter
    {
        public int StartCount { get; private set; }
        public List<ProcessStartRequest> Requests { get; } = [];

        public Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken)
        {
            StartCount++;
            Requests.Add(request);
            if (outputLine is not null)
            {
                request.OutputReceived?.Invoke(outputLine);
            }

            var selectedProcessId = processIds is not null && StartCount <= processIds.Count
                ? processIds[StartCount - 1]
                : processId;
            return Task.FromResult(new ProcessStartResult(selectedProcessId));
        }
    }

    private sealed class RecordingProcessStopper : IProcessStopper
    {
        public int? StoppedProcessId { get; private set; }

        public Task<ProcessStopResult> StopAsync(int processId, CancellationToken cancellationToken)
        {
            StoppedProcessId = processId;
            return Task.FromResult(new ProcessStopResult(true, $"Процесс {processId} остановлен."));
        }
    }

    private sealed class ThrowingProcessStarter(string message) : IProcessStarter
    {
        public Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException(message);
    }

    private sealed class MissingExecutableResolver : IExecutableResolver
    {
        public Task<string?> FindExecutableAsync(string executableName, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        public Task<string?> GetVersionAsync(string executableName, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);
    }

    private sealed class FixedEndpointHealthClient(EndpointHealthResult result) : IEndpointHealthClient
    {
        public Task<EndpointHealthResult> WaitUntilReadyAsync(
            string baseUrl,
            int Attempts,
            TimeSpan Delay,
            CancellationToken cancellationToken) =>
            Task.FromResult(result);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "runtime-compat-flow-" + Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
            using var file = File.Create(System.IO.Path.Combine(Path, "Qwen3-Coder-Q4_K_M.gguf"));
            file.SetLength(129L * 1024L * 1024L);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class EmptyTempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "runtime-compat-empty-" + Guid.NewGuid().ToString("N"));

        public EmptyTempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
