using Launcher.Core.Scenarios;
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

public sealed class MtpSettingsFlowTests
{
    [Fact]
    public async Task BuildLaunchCommandUsesUserSelectedMtpDraftTokenLimit()
    {
        using var temp = new TempDirectory();
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(
                new EmptyGpuProbe(),
                new EmptyPortInspector(),
                new FakeRuntimeCatalog([Runtime(supportsMtp: true, supportsTurboQuant: false)])),
            new RuntimeStartCoordinator(
                new EmptyPortInspector(),
                new EmptyPortReleaser(),
                new EmptyProcessStarter()),
            new EmptyDownloadService());
        viewModel.FolderPicker = new FixedFolderPicker(temp.Path);
        viewModel.SelectedRuntime = RuntimeKind.LlamaCppMtp;
        viewModel.SelectEndpointModeCommand.Execute(null);
        viewModel.SelectedDecodingPreset = viewModel.DecodingPresets.Single(preset => preset.Id == "mtp-fast");
        viewModel.MtpDraftTokens = 2;

        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        viewModel.BuildLaunchCommandCommand.Execute(null);

        Assert.Contains("--spec-draft-n-max 2", viewModel.LaunchCommandPreview);
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

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mtp-settings-flow-" + Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
            using var file = File.Create(System.IO.Path.Combine(Path, "Qwen3-Coder-7B-Instruct-Q4_K_M.gguf"));
            file.SetLength(256L * 1024L * 1024L);
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
