using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Launcher.Runtimes.LlamaCpp;

public sealed class GitHubReleaseClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<GitHubRelease>> ListAsync(
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(owner))
        {
            throw new ArgumentException("GitHub owner is required.", nameof(owner));
        }

        if (string.IsNullOrWhiteSpace(repository))
        {
            throw new ArgumentException("GitHub repository is required.", nameof(repository));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri($"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/releases"));
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.UserAgent.ParseAdd("AI-Launcher-Studio/0.1");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var releases = await response.Content.ReadFromJsonAsync<IReadOnlyList<GitHubReleaseDto>>(cancellationToken)
            ?? [];
        return releases.Select(MapRelease).ToArray();
    }

    private static GitHubRelease MapRelease(GitHubReleaseDto release) => new(
        release.TagName ?? "",
        release.Name ?? release.TagName ?? "",
        release.Draft,
        release.Prerelease,
        release.PublishedAt,
        (release.Assets ?? []).Select(MapAsset).ToArray());

    private static GitHubReleaseAsset MapAsset(GitHubReleaseAssetDto asset) => new(
        asset.Name ?? "",
        asset.DownloadUrl ?? new Uri("about:blank"),
        asset.ContentType,
        asset.SizeBytes);

    private sealed record GitHubReleaseDto(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("prerelease")] bool Prerelease,
        [property: JsonPropertyName("published_at")] DateTimeOffset? PublishedAt,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubReleaseAssetDto>? Assets);

    private sealed record GitHubReleaseAssetDto(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("browser_download_url")] Uri? DownloadUrl,
        [property: JsonPropertyName("content_type")] string? ContentType,
        [property: JsonPropertyName("size")] long SizeBytes);
}
