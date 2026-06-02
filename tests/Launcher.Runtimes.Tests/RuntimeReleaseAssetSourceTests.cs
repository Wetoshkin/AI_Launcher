using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimeReleaseAssetSourceTests
{
    [Theory]
    [InlineData("stable", RuntimeReleaseAssetSource.Stable)]
    [InlineData(" Stable Release ", RuntimeReleaseAssetSource.Stable)]
    [InlineData("latest", RuntimeReleaseAssetSource.Latest)]
    [InlineData("latest-release", RuntimeReleaseAssetSource.Latest)]
    [InlineData("manual", RuntimeReleaseAssetSource.Manual)]
    [InlineData("manually_selected", RuntimeReleaseAssetSource.Manual)]
    [InlineData("detected", RuntimeReleaseAssetSource.Detected)]
    [InlineData("auto detected", RuntimeReleaseAssetSource.Detected)]
    public void NormalizeMapsKnownSourceNames(string value, RuntimeReleaseAssetSource expected)
    {
        Assert.Equal(expected, RuntimeReleaseAssetSources.Normalize(value));
    }

    [Theory]
    [InlineData(RuntimeReleaseAssetSource.Stable, "стабильный релиз")]
    [InlineData(RuntimeReleaseAssetSource.Latest, "последний релиз")]
    [InlineData(RuntimeReleaseAssetSource.Manual, "выбран вручную")]
    [InlineData(RuntimeReleaseAssetSource.Detected, "обнаружен автоматически")]
    public void ToLabelReturnsHumanReadableLabel(RuntimeReleaseAssetSource source, string expected)
    {
        Assert.Equal(expected, RuntimeReleaseAssetSources.ToLabel(source));
    }

    [Fact]
    public void SelectZipPackagesMarksStableChannelByDefault()
    {
        var packages = RuntimeReleaseAssetSelector.SelectZipPackages(
            [Release("b5400", prerelease: false)],
            ["win", "cuda", "x64"]);

        var package = Assert.Single(packages);
        Assert.Equal(RuntimeReleaseAssetSource.Stable, package.Source);
        Assert.Equal("стабильный релиз", package.SourceLabel);
    }

    [Fact]
    public void SelectZipPackagesMarksLatestChannelWhenPrereleasesAreIncluded()
    {
        var packages = RuntimeReleaseAssetSelector.SelectZipPackages(
            [Release("b5401", prerelease: true)],
            ["win", "cuda", "x64"],
            includePrerelease: true);

        var package = Assert.Single(packages);
        Assert.Equal(RuntimeReleaseAssetSource.Latest, package.Source);
        Assert.Equal("последний релиз", package.SourceLabel);
    }

    private static GitHubRelease Release(string tagName, bool prerelease) => new(
        tagName,
        $"Release {tagName}",
        Draft: false,
        prerelease,
        PublishedAt: new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.Zero),
        [new GitHubReleaseAsset(
            $"llama-{tagName}-bin-win-cuda-x64.zip",
            new Uri("https://github.com/runtime.zip"),
            "application/zip",
            SizeBytes: 42)]);
}
