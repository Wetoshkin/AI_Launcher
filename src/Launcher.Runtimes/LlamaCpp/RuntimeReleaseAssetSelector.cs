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
            .SelectMany(release =>
            {
                // CUDA-сборкам нужны библиотеки cudart — кладём их URL рядом, чтобы докачать при установке.
                var cudart = release.Assets.FirstOrDefault(a =>
                    IsZip(a) && IsCudart(a.Name) && a.Name.Contains("x64", StringComparison.OrdinalIgnoreCase));

                return release.Assets
                    .Where(asset => IsZip(asset))
                    .Where(asset => !IsCudart(asset.Name)) // cudart — не самостоятельный runtime, исключаем из списка
                    .Where(asset => MatchesFragments(asset.Name, requiredNameFragments))
                    .Select(asset => new RuntimeReleasePackage(
                        release.TagName,
                        release.Name,
                        release.PublishedAt,
                        asset.Name,
                        asset.DownloadUrl,
                        asset.SizeBytes,
                        release.Prerelease,
                        source,
                        asset.Name.Contains("cuda", StringComparison.OrdinalIgnoreCase) ? cudart?.DownloadUrl : null));
            })
            .ToArray();
    }

    private static bool IsCudart(string name) =>
        name.Contains("cudart", StringComparison.OrdinalIgnoreCase);

    private static bool IsZip(GitHubReleaseAsset asset) =>
        asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
        || string.Equals(asset.ContentType, "application/zip", StringComparison.OrdinalIgnoreCase)
        || string.Equals(asset.ContentType, "application/x-zip-compressed", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesFragments(string name, IReadOnlyList<string> fragments) =>
        fragments.Count == 0
        || fragments.All(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
