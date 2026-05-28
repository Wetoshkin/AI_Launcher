namespace Launcher.Runtimes.LlamaCpp;

public interface IRuntimePackageInstaller
{
    Task<RuntimePackageInstallResult> InstallAsync(
        RuntimePackageInstallRequest request,
        CancellationToken cancellationToken);
}
