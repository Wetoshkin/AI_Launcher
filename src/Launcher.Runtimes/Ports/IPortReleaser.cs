namespace Launcher.Runtimes.Ports;

public interface IPortReleaser
{
    Task<PortReleaseResult> ReleaseIfSafeAsync(PortOwnerInfo portOwner, CancellationToken cancellationToken);
}
