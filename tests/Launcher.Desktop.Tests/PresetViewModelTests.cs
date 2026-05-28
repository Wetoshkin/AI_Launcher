using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;
using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;
using Launcher.Runtimes.Hardware;
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
        Assert.Equal("Пресет применён: OpenCode + Gemma.", viewModel.StatusMessage);
    }

    private sealed class EmptyDownloadService : IHuggingFaceModelDownloadService
    {
        public Task<HuggingFaceModelDownloadResult> DownloadAsync(
            HuggingFaceModelDownloadRequest request,
            CancellationToken cancellationToken,
            Action<HuggingFaceDownloadProgress>? progress = null) =>
            Task.FromResult(new HuggingFaceModelDownloadResult([], []));
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
