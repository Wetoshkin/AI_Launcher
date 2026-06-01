namespace Launcher.Runtimes.LlamaCpp;

public sealed class RuntimeReleaseCatalogService(
    GitHubReleaseClient releaseClient,
    string owner = "ggerganov",
    string repository = "llama.cpp",
    IReadOnlyList<string>? requiredNameFragments = null) : IRuntimeReleaseCatalog
{
    public async Task<IReadOnlyList<RuntimeReleasePackage>> ListPackagesAsync(CancellationToken cancellationToken)
    {
        var releases = await releaseClient.ListAsync(owner, repository, cancellationToken);
        return RuntimeReleaseAssetSelector.SelectZipPackages(
            releases,
            requiredNameFragments ?? ["win", "x64"]);
    }
}
