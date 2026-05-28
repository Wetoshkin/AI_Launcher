using System.Diagnostics;

namespace Launcher.Agents.Discovery;

public sealed class WindowsExecutableResolver : IExecutableResolver
{
    public async Task<string?> FindExecutableAsync(string executableName, CancellationToken cancellationToken)
    {
        var output = await RunAsync("where.exe", executableName, cancellationToken);
        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }

    public async Task<string?> GetVersionAsync(string executableName, CancellationToken cancellationToken)
    {
        var output = await RunAsync(executableName, "--version", cancellationToken);
        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
    }

    private static async Task<string> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0 ? stdout : stderr;
        }
        catch
        {
            return "";
        }
    }
}
