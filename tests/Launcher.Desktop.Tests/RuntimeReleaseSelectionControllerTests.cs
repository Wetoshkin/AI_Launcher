using Launcher.Desktop.ViewModels;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.Tests;

public sealed class RuntimeReleaseSelectionControllerTests
{
    [Fact]
    public void ProfileHintUsesRussianHardwareExplanation()
    {
        Assert.Equal(
            "CUDA: для видеокарт NVIDIA, обычно самый быстрый вариант для RTX.",
            RuntimeReleaseSelectionController.ProfileHint(RuntimeReleaseProfile.Cuda));
        Assert.Equal(
            "Процессор: запуск без ускорения видеокартой, самый совместимый вариант.",
            RuntimeReleaseSelectionController.ProfileHint(RuntimeReleaseProfile.Cpu));
    }

    [Fact]
    public void BuildPackageRowsFiltersBySourceAndLimitsResults()
    {
        var packages = Enumerable.Range(1, 20)
            .Select(index => Package(
                $"b{index:0000}",
                RuntimeReleaseAssetSource.Stable))
            .Concat([Package("latest", RuntimeReleaseAssetSource.Latest)])
            .ToArray();

        var rows = RuntimeReleaseSelectionController.BuildPackageRows(
            packages,
            RuntimeReleaseAssetSource.Stable);

        Assert.Equal(12, rows.Count);
        Assert.All(rows, row => Assert.Equal(RuntimeReleaseAssetSource.Stable, row.Package.Source));
        Assert.DoesNotContain(rows, row => row.Package.TagName == "latest");
    }

    [Fact]
    public void SourceOptionsExposeStableLatestManualAndDetected()
    {
        var sources = RuntimeReleaseSelectionController.SourceOptions
            .Select(option => option.Source)
            .ToArray();

        Assert.Equal(
            [
                RuntimeReleaseAssetSource.Stable,
                RuntimeReleaseAssetSource.Latest,
                RuntimeReleaseAssetSource.Manual,
                RuntimeReleaseAssetSource.Detected
            ],
            sources);
    }

    private static RuntimeReleasePackage Package(string tagName, RuntimeReleaseAssetSource source) =>
        new(
            tagName,
            ReleaseName: tagName,
            PublishedAt: DateTimeOffset.Parse("2026-06-03T00:00:00Z"),
            AssetName: $"llama-{tagName}-bin-win-cuda-x64.zip",
            DownloadUrl: new Uri($"https://example.test/{tagName}.zip"),
            SizeBytes: 128_000_000,
            Prerelease: false,
            Source: source);
}
