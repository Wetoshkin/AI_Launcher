using System.Net;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimeReleaseDownloadServiceTests
{
    [Fact]
    public async Task DownloadAsyncWritesAssetIntoTagCacheFolder()
    {
        using var temp = new TempDirectory();
        var service = new RuntimeReleaseDownloadService(new HttpClient(new StaticContentHandler("runtime zip")));
        var progressEvents = new List<RuntimeReleaseDownloadProgress>();

        var result = await service.DownloadAsync(
            new RuntimeReleaseDownloadRequest(Package("b5400", "llama-b5400-win-x64.zip", sizeBytes: 11), temp.Path),
            CancellationToken.None,
            progressEvents.Add);

        var expectedPath = System.IO.Path.Combine(temp.Path, "b5400", "llama-b5400-win-x64.zip");
        Assert.True(result.Downloaded);
        Assert.False(result.Skipped);
        Assert.Equal(expectedPath, result.ArchivePath);
        Assert.Equal("runtime zip", await File.ReadAllTextAsync(expectedPath));
        Assert.False(File.Exists(expectedPath + ".download"));
        var finalProgress = Assert.Single(progressEvents.Where(progress => progress.BytesReceived == progress.TotalBytes));
        Assert.Equal("llama-b5400-win-x64.zip", finalProgress.AssetName);
        Assert.Equal(11, finalProgress.BytesReceived);
        Assert.Equal(11, finalProgress.TotalBytes);
        Assert.False(finalProgress.IsSkipped);
    }

    [Fact]
    public async Task DownloadAsyncSkipsExistingArchiveWithMatchingSize()
    {
        using var temp = new TempDirectory();
        var targetDirectory = System.IO.Path.Combine(temp.Path, "b5400");
        Directory.CreateDirectory(targetDirectory);
        var targetPath = System.IO.Path.Combine(targetDirectory, "llama.zip");
        await File.WriteAllTextAsync(targetPath, "cached");
        var handler = new StaticContentHandler("network");
        var service = new RuntimeReleaseDownloadService(new HttpClient(handler));
        var progressEvents = new List<RuntimeReleaseDownloadProgress>();

        var result = await service.DownloadAsync(
            new RuntimeReleaseDownloadRequest(Package("b5400", "llama.zip", sizeBytes: 6), temp.Path),
            CancellationToken.None,
            progressEvents.Add);

        Assert.True(result.Skipped);
        Assert.False(result.Downloaded);
        Assert.Equal(0, handler.RequestCount);
        Assert.Equal("cached", await File.ReadAllTextAsync(targetPath));
        var progress = Assert.Single(progressEvents);
        Assert.True(progress.IsSkipped);
        Assert.Equal(6, progress.BytesReceived);
    }

    [Fact]
    public async Task DownloadAsyncRejectsUnsafeAssetName()
    {
        using var temp = new TempDirectory();
        var service = new RuntimeReleaseDownloadService(new HttpClient(new StaticContentHandler("runtime zip")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadAsync(
            new RuntimeReleaseDownloadRequest(Package("b5400", @"..\evil.zip", sizeBytes: 11), temp.Path),
            CancellationToken.None));
    }

    private static RuntimeReleasePackage Package(string tag, string assetName, long sizeBytes) => new(
        tag,
        $"Release {tag}",
        PublishedAt: new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.Zero),
        assetName,
        new Uri("https://github.com/runtime.zip"),
        sizeBytes,
        Prerelease: false);

    private sealed class StaticContentHandler(string content) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "runtime-release-download-" + Guid.NewGuid().ToString("N"));

        public TempDirectory()
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
