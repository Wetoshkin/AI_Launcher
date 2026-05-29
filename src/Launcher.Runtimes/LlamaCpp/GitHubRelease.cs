namespace Launcher.Runtimes.LlamaCpp;

public sealed record GitHubRelease(
    string TagName,
    string Name,
    bool Draft,
    bool Prerelease,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<GitHubReleaseAsset> Assets);

public sealed record GitHubReleaseAsset(
    string Name,
    Uri DownloadUrl,
    string? ContentType,
    long SizeBytes);

public sealed record RuntimeReleasePackage(
    string TagName,
    string ReleaseName,
    DateTimeOffset? PublishedAt,
    string AssetName,
    Uri DownloadUrl,
    long SizeBytes,
    bool Prerelease);
