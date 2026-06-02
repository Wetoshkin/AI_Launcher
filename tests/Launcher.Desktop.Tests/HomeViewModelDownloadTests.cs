using Launcher.Core.LaunchPlans;
using Launcher.Desktop.Services;
using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;
using Launcher.Runtimes.Startup;
using Launcher.Runtimes.Status;

namespace Launcher.Desktop.Tests;

public sealed class HomeViewModelDownloadTests
{
    [Fact]
    public async Task SearchHuggingFaceCommandFiltersRemoteModelsBySelectedQuant()
    {
        var handler = new JsonHttpHandler("""
        [
          {
            "id": "unsloth/Qwen3-Coder-GGUF",
            "downloads": 100,
            "likes": 10,
            "tags": ["gguf"],
            "siblings": [{"rfilename": "Qwen3-Coder-Q4_K_M.gguf"}]
          },
          {
            "id": "unsloth/Qwen3-Coder-Q8-GGUF",
            "downloads": 90,
            "likes": 9,
            "tags": ["gguf"],
            "siblings": [{"rfilename": "Qwen3-Coder-Q8_0.gguf"}]
          }
        ]
        """);
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(handler) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            new FakeDownloadService())
        {
            HfSearchText = "qwen coder",
            HfQuantFilter = "Q4_K_M"
        };

        await viewModel.SearchHuggingFaceCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.RemoteModels);
        Assert.Equal("unsloth/Qwen3-Coder-GGUF", row.Id);
        Assert.Equal("Hugging Face: найдено 1 моделей.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task SearchHuggingFaceCommandFiltersRemoteModelsBySelectedFamily()
    {
        var handler = new JsonHttpHandler("""
        [
          {
            "id": "unsloth/Qwen3-Coder-GGUF",
            "downloads": 100,
            "likes": 10,
            "tags": ["gguf", "qwen"],
            "siblings": [{"rfilename": "Qwen3-Coder-Q4_K_M.gguf"}]
          },
          {
            "id": "unsloth/DeepSeek-Coder-GGUF",
            "downloads": 90,
            "likes": 9,
            "tags": ["gguf", "deepseek"],
            "siblings": [{"rfilename": "DeepSeek-Coder-Q4_K_M.gguf"}]
          }
        ]
        """);
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(handler) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            new FakeDownloadService())
        {
            HfSearchText = "coder",
            HfFamilyFilter = "Qwen"
        };

        await viewModel.SearchHuggingFaceCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.RemoteModels);
        Assert.Equal("unsloth/Qwen3-Coder-GGUF", row.Id);
    }

    [Fact]
    public async Task SearchHuggingFaceCommandExposesDownloadOptionSizes()
    {
        var handler = new JsonHttpHandler("""
        [
          {
            "id": "unsloth/Qwen3-Coder-GGUF",
            "downloads": 100,
            "likes": 10,
            "tags": ["gguf"],
            "siblings": [
              {"rfilename": "Qwen3-Coder-Q4_K_M.gguf", "size": 4294967296},
              {"rfilename": "Qwen3-Coder-Q5_K_M.gguf"}
            ]
          }
        ]
        """);
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(handler) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            new FakeDownloadService())
        {
            HfSearchText = "qwen coder"
        };

        await viewModel.SearchHuggingFaceCommand.ExecuteAsync(null);

        Assert.Collection(viewModel.RemoteDownloadOptions,
            option =>
            {
                Assert.Equal("Qwen3-Coder-Q4_K_M.gguf", option.Label);
                Assert.Equal(4_294_967_296, option.TotalSizeBytes);
                Assert.Equal("4 GB", option.SizeText);
            },
            option =>
            {
                Assert.Equal("Qwen3-Coder-Q5_K_M.gguf", option.Label);
                Assert.Null(option.TotalSizeBytes);
                Assert.Equal("", option.SizeText);
            });
    }

    [Fact]
    public async Task SearchHuggingFaceCommandFiltersRemoteModelsBySelectedSize()
    {
        var handler = new JsonHttpHandler("""
        [
          {
            "id": "unsloth/Qwen3-Coder-4GB-GGUF",
            "downloads": 100,
            "likes": 10,
            "tags": ["gguf"],
            "siblings": [{"rfilename": "Qwen3-Coder-Q4_K_M.gguf", "size": 4294967296}]
          },
          {
            "id": "unsloth/Qwen3-Coder-12GB-GGUF",
            "downloads": 90,
            "likes": 9,
            "tags": ["gguf"],
            "siblings": [{"rfilename": "Qwen3-Coder-Q8_0.gguf", "size": 12884901888}]
          },
          {
            "id": "unsloth/Qwen3-Coder-Unknown-GGUF",
            "downloads": 80,
            "likes": 8,
            "tags": ["gguf"],
            "siblings": [{"rfilename": "Qwen3-Coder-Q5_K_M.gguf"}]
          }
        ]
        """);
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(handler) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            new FakeDownloadService())
        {
            HfSearchText = "qwen coder",
            HfSizeFilter = "до 8 ГБ"
        };

        await viewModel.SearchHuggingFaceCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.RemoteModels);
        Assert.Equal("unsloth/Qwen3-Coder-4GB-GGUF", row.Id);
        Assert.Single(viewModel.RemoteDownloadOptions);
        Assert.Equal(4_294_967_296, viewModel.RemoteDownloadOptions.Single().TotalSizeBytes);
    }

    [Fact]
    public async Task DownloadSelectedRemoteModelCommandPassesSelectedOptionToDownloadService()
    {
        using var temp = new TempDirectory();
        var downloadService = new FakeDownloadService();
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            downloadService)
        {
            FolderPicker = new FixedFolderPicker(temp.Path)
        };
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        var remoteModel = new RemoteModelRowViewModel(new HuggingFaceModelSummary(
            "unsloth/Qwen3-Coder-GGUF",
            Downloads: 100,
            Likes: 10,
            Tags: ["gguf"],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFiles: ["Qwen3-Coder-Q4_K_M.gguf"]));

        viewModel.RemoteModels.Add(remoteModel);
        viewModel.SelectedRemoteModel = remoteModel;
        viewModel.SelectedRemoteDownloadOption = viewModel.RemoteDownloadOptions.Single();
        await viewModel.DownloadSelectedRemoteModelCommand.ExecuteAsync(null);

        Assert.NotNull(downloadService.LastRequest);
        Assert.Equal("unsloth/Qwen3-Coder-GGUF", downloadService.LastRequest.RepoId);
        Assert.Equal(temp.Path, downloadService.LastRequest.ModelsDirectory);
        Assert.Equal("Qwen3-Coder-Q4_K_M.gguf", downloadService.LastRequest.Option.Label);
        Assert.Equal("Скачивание завершено: 1 скачано, 0 уже были на диске.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DownloadSelectedRemoteModelCommandUpdatesProgressState()
    {
        using var temp = new TempDirectory();
        var downloadService = new FakeDownloadService
        {
            ProgressToEmit = new HuggingFaceDownloadProgress(
                "Qwen3-Coder-Q4_K_M.gguf",
                FileIndex: 1,
                TotalFiles: 2,
                BytesReceived: 50,
                TotalBytes: 100,
                IsSkipped: false)
        };
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            downloadService)
        {
            FolderPicker = new FixedFolderPicker(temp.Path)
        };
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        var remoteModel = new RemoteModelRowViewModel(new HuggingFaceModelSummary(
            "unsloth/Qwen3-Coder-GGUF",
            Downloads: 100,
            Likes: 10,
            Tags: ["gguf"],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFiles: ["Qwen3-Coder-Q4_K_M.gguf"]));

        viewModel.SelectedRemoteModel = remoteModel;
        viewModel.SelectedRemoteDownloadOption = viewModel.RemoteDownloadOptions.Single();
        await viewModel.DownloadSelectedRemoteModelCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsDownloading);
        Assert.Equal(50, viewModel.DownloadProgressPercent);
        Assert.Equal("1/2 · Qwen3-Coder-Q4_K_M.gguf · 50%", viewModel.DownloadProgressText);
    }

    [Fact]
    public async Task CancelDownloadCommandCancelsActiveDownload()
    {
        using var temp = new TempDirectory();
        var downloadService = new BlockingDownloadService();
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            downloadService)
        {
            FolderPicker = new FixedFolderPicker(temp.Path)
        };
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);
        var remoteModel = new RemoteModelRowViewModel(new HuggingFaceModelSummary(
            "unsloth/Qwen3-Coder-GGUF",
            Downloads: 100,
            Likes: 10,
            Tags: ["gguf"],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFiles: ["Qwen3-Coder-Q4_K_M.gguf"]));

        viewModel.SelectedRemoteModel = remoteModel;
        viewModel.SelectedRemoteDownloadOption = viewModel.RemoteDownloadOptions.Single();
        var running = viewModel.DownloadSelectedRemoteModelCommand.ExecuteAsync(null);
        await downloadService.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        viewModel.CancelDownloadCommand.Execute(null);
        await running;

        Assert.True(downloadService.WasCanceled);
        Assert.False(viewModel.IsDownloading);
        Assert.Equal("Скачивание отменено.", viewModel.StatusMessage);
    }

    private sealed class FakeDownloadService : IHuggingFaceModelDownloadService
    {
        public HuggingFaceModelDownloadRequest? LastRequest { get; private set; }

        public HuggingFaceDownloadProgress? ProgressToEmit { get; set; }

        public Task<HuggingFaceModelDownloadResult> DownloadAsync(
            HuggingFaceModelDownloadRequest request,
            CancellationToken cancellationToken,
            Action<HuggingFaceDownloadProgress>? progress = null)
        {
            LastRequest = request;
            if (ProgressToEmit is not null)
            {
                progress?.Invoke(ProgressToEmit);
            }

            return Task.FromResult(new HuggingFaceModelDownloadResult(["downloaded.gguf"], []));
        }
    }

    private sealed class BlockingDownloadService : IHuggingFaceModelDownloadService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WasCanceled { get; private set; }

        public async Task<HuggingFaceModelDownloadResult> DownloadAsync(
            HuggingFaceModelDownloadRequest request,
            CancellationToken cancellationToken,
            Action<HuggingFaceDownloadProgress>? progress = null)
        {
            Started.SetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                WasCanceled = true;
                throw;
            }

            return new HuggingFaceModelDownloadResult([], []);
        }
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

    private sealed class JsonHttpHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });
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
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "launcher-desktop-" + Guid.NewGuid().ToString("N"));

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
