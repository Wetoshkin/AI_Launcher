namespace Launcher.Runtimes.Processes;

public interface IProcessStarter
{
    Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken);
}
