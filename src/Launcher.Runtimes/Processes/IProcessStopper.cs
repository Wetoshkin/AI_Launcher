namespace Launcher.Runtimes.Processes;

public interface IProcessStopper
{
    Task<ProcessStopResult> StopAsync(int processId, CancellationToken cancellationToken);
}
