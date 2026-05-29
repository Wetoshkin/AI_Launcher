using System.Net;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class GitHubReleaseClientTests
{
    [Fact]
    public async Task ListAsyncUsesGitHubReleasesEndpointAndParsesAssets()
    {
        var handler = new CapturingHandler("""
        [
          {
            "tag_name": "b5400",
            "name": "llama.cpp b5400",
            "draft": false,
            "prerelease": false,
            "published_at": "2026-05-28T10:00:00Z",
            "assets": [
              {
                "name": "llama-b5400-bin-win-cuda-cu12.4-x64.zip",
                "browser_download_url": "https://github.com/ggerganov/llama.cpp/releases/download/b5400/llama.zip",
                "content_type": "application/zip",
                "size": 123456789
              }
            ]
          }
        ]
        """);
        var client = new GitHubReleaseClient(new HttpClient(handler));

        var releases = await client.ListAsync("ggerganov", "llama.cpp", CancellationToken.None);

        Assert.Equal("https://api.github.com/repos/ggerganov/llama.cpp/releases", handler.LastRequestUri?.ToString());
        Assert.Contains(handler.LastHeaders, header => header.Key == "Accept" && header.Value.Contains("application/vnd.github+json"));
        Assert.Contains(handler.LastHeaders, header => header.Key == "X-GitHub-Api-Version" && header.Value.Contains("2022-11-28"));
        var release = Assert.Single(releases);
        Assert.Equal("b5400", release.TagName);
        Assert.Equal(new DateTimeOffset(2026, 5, 28, 10, 0, 0, TimeSpan.Zero), release.PublishedAt);
        var asset = Assert.Single(release.Assets);
        Assert.Equal("llama-b5400-bin-win-cuda-cu12.4-x64.zip", asset.Name);
        Assert.Equal(123456789, asset.SizeBytes);
    }

    [Fact]
    public void RuntimeReleaseAssetSelectorSkipsDraftsPrereleasesAndNonZipAssets()
    {
        var packages = RuntimeReleaseAssetSelector.SelectZipPackages(
        [
            Release("b3", draft: false, prerelease: true, Asset("pre.zip")),
            Release("b2", draft: true, prerelease: false, Asset("draft.zip")),
            Release("b1", draft: false, prerelease: false, Asset("notes.txt")),
            Release("b0", draft: false, prerelease: false, Asset("llama-bin-win-cuda-x64.zip"))
        ], ["win", "cuda"]);

        var package = Assert.Single(packages);
        Assert.Equal("b0", package.TagName);
        Assert.Equal("llama-bin-win-cuda-x64.zip", package.AssetName);
        Assert.Equal(new Uri("https://github.com/runtime.zip"), package.DownloadUrl);
    }

    private static GitHubRelease Release(
        string tag,
        bool draft,
        bool prerelease,
        params GitHubReleaseAsset[] assets) => new(
        tag,
        $"Release {tag}",
        draft,
        prerelease,
        PublishedAt: new DateTimeOffset(2026, 5, 20, 0, 0, 0, TimeSpan.Zero),
        assets);

    private static GitHubReleaseAsset Asset(string name) => new(
        name,
        new Uri("https://github.com/runtime.zip"),
        "application/zip",
        SizeBytes: 42);

    private sealed class CapturingHandler(string json) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        public IReadOnlyList<KeyValuePair<string, string>> LastHeaders { get; private set; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastHeaders = request.Headers
                .Select(header => new KeyValuePair<string, string>(header.Key, string.Join(",", header.Value)))
                .ToArray();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
        }
    }
}
