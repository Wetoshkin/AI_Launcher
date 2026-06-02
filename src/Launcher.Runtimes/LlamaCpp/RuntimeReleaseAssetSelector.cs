namespace Launcher.Runtimes.LlamaCpp;

public static class RuntimeReleaseAssetSelector
{
    public static IReadOnlyList<RuntimeReleasePackage> SelectZipPackages(
        IEnumerable<GitHubRelease> releases,
        IReadOnlyList<string> requiredNameFragments,
        bool includePrerelease = false)
    {
        var source = includePrerelease ? RuntimeReleaseAssetSource.Latest : RuntimeReleaseAssetSource.Stable;
        return releases
            .Where(release => !release.Draft)
            .Where(release => includePrerelease || !release.Prerelease)
            .OrderByDescending(release => release.PublishedAt)
            .SelectMany(release => release.Assets
                .Where(asset => IsZip(asset))
                .Where(asset => MatchesFragments(asset.Name, requiredNameFragments))
                .Select(asset => new RuntimeReleasePackage(
                    release.TagName,
                    release.Name,
                    release.PublishedAt,
                    asset.Name,
                    asset.DownloadUrl,
                    asset.SizeBytes,
                    release.Prerelease,
                    source)))
            .ToArray();
    }

    private static bool IsZip(GitHubReleaseAsset asset) =>
        asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
        || string.Equals(asset.ContentType, "application/zip", StringComparison.OrdinalIgnoreCase)
        || string.Equals(asset.ContentType, "application/x-zip-compressed", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesFragments(string name, IReadOnlyList<string> fragments) =>
        fragments.Count == 0
        || fragments.All(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
