namespace Launcher.Runtimes.LlamaCpp;

public sealed class RuntimeReleaseCatalogService(
    GitHubReleaseClient releaseClient,
    string owner = "ggerganov",
    string repository = "llama.cpp") : IRuntimeReleaseCatalog
{
    public async Task<IReadOnlyList<RuntimeReleasePackage>> ListPackagesAsync(
        RuntimeReleaseProfile profile,
        CancellationToken cancellationToken)
    {
        var releases = await releaseClient.ListAsync(owner, repository, cancellationToken);
        return RuntimeReleaseAssetSelector.SelectZipPackages(
            releases,
            RuntimeReleaseProfileFragments.For(profile));
    }
}
