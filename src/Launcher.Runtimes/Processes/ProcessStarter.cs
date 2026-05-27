using System.Diagnostics;

namespace Launcher.Runtimes.Processes;

public sealed class ProcessStarter : IProcessStarter
{
    public Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(request.Executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var item in request.Environment)
        {
            startInfo.Environment[item.Key] = item.Value;
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Не удалось запустить {request.Executable}.");
        return Task.FromResult(new ProcessStartResult(process.Id));
    }
}
