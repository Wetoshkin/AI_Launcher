namespace Launcher.Runtimes.LlamaCpp;

public interface IRuntimeReleaseCatalog
{
    Task<IReadOnlyList<RuntimeReleasePackage>> ListPackagesAsync(
        RuntimeReleaseProfile profile,
        CancellationToken cancellationToken);
}
