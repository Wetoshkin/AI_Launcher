namespace Launcher.Agents.Discovery;

public interface IExecutableResolver
{
    Task<string?> FindExecutableAsync(string executableName, CancellationToken cancellationToken);

    Task<string?> GetVersionAsync(string executableName, CancellationToken cancellationToken);
}
