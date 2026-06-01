using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimeUpdateServiceTests
{
    [Fact]
    public void CheckReportsUpToDateWhenArchivePathContainsLatestTag()
    {
        var result = RuntimeUpdateService.Check(
            @"D:\AI\cache\b5400\llama-b5400-bin-win-cuda-x64.zip",
            [Package("b5400", "llama-b5400-bin-win-cuda-x64.zip")]);

        Assert.False(result.UpdateAvailable);
        Assert.Equal("runtime актуален: b5400", result.Message);
    }

    [Fact]
    public void CheckReportsAvailableUpdateWhenArchiveTagIsOlder()
    {
        var result = RuntimeUpdateService.Check(
            @"D:\AI\cache\b5300\llama-b5300-bin-win-cuda-x64.zip",
            [Package("b5400", "llama-b5400-bin-win-cuda-x64.zip")]);

        Assert.True(result.UpdateAvailable);
        Assert.Equal("доступно обновление: b5300 -> b5400", result.Message);
    }

    [Fact]
    public void CheckAsksForRuntimeArchiveWhenPathIsEmpty()
    {
        var result = RuntimeUpdateService.Check("", [Package("b5400", "llama-b5400-bin-win-cuda-x64.zip")]);

        Assert.False(result.UpdateAvailable);
        Assert.Equal("укажите или скачайте архив runtime", result.Message);
    }

    private static RuntimeReleasePackage Package(string tag, string assetName) => new(
        tag,
        $"Release {tag}",
        PublishedAt: new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.Zero),
        assetName,
        new Uri("https://github.com/runtime.zip"),
        SizeBytes: 42,
        Prerelease: false);
}
