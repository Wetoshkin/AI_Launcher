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
            RedirectStandardOutput = request.OutputReceived is not null,
            RedirectStandardError = request.OutputReceived is not null,
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
        if (request.OutputReceived is not null)
        {
            _ = Task.Run(async () => await ReadProcessOutputAsync(process, request.OutputReceived, cancellationToken), cancellationToken);
        }

        return Task.FromResult(new ProcessStartResult(process.Id));
    }

    private static async Task ReadProcessOutputAsync(
        Process process,
        Action<string> outputReceived,
        CancellationToken cancellationToken)
    {
        async Task ReadStreamAsync(StreamReader reader, string prefix)
        {
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    outputReceived($"{prefix}{line}");
                }
            }
        }

        await Task.WhenAll(
            ReadStreamAsync(process.StandardOutput, ""),
            ReadStreamAsync(process.StandardError, "ERR: "));
    }
}
