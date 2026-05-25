namespace Launcher.Runtimes.Ports;

public interface ICommandRunner
{
    Task<string> RunAsync(string fileName, string arguments, CancellationToken cancellationToken);
}
