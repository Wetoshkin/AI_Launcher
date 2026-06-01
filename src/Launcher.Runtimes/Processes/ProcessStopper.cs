using System.Diagnostics;

namespace Launcher.Runtimes.Processes;

public sealed class ProcessStopper : IProcessStopper
{
    public Task<ProcessStopResult> StopAsync(int processId, CancellationToken cancellationToken)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill(entireProcessTree: true);
            return Task.FromResult(new ProcessStopResult(true, $"Процесс {processId} остановлен."));
        }
        catch (ArgumentException)
        {
            return Task.FromResult(new ProcessStopResult(false, $"Процесс {processId} уже не найден."));
        }
    }
}
