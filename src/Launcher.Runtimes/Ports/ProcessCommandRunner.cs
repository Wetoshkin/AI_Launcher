using System.Diagnostics;

namespace Launcher.Runtimes.Ports;

public sealed class ProcessCommandRunner : ICommandRunner
{
    public async Task<string> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }
}
