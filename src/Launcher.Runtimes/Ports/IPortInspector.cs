namespace Launcher.Runtimes.Ports;

public interface IPortInspector
{
    Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken);
}
