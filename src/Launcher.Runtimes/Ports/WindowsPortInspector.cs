namespace Launcher.Runtimes.Ports;

public sealed class WindowsPortInspector : IPortInspector
{
    public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken)
    {
        return Task.FromResult<PortOwnerInfo?>(null);
    }
}
