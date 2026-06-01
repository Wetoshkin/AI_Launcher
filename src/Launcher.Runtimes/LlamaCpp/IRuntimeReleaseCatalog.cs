namespace Launcher.Runtimes.LlamaCpp;

public interface IRuntimeReleaseCatalog
{
    Task<IReadOnlyList<RuntimeReleasePackage>> ListPackagesAsync(CancellationToken cancellationToken);
}
