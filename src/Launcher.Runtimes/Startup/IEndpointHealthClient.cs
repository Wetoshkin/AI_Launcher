namespace Launcher.Runtimes.Startup;

public interface IEndpointHealthClient
{
    Task<EndpointHealthResult> WaitUntilReadyAsync(
        string baseUrl,
        int Attempts,
        TimeSpan Delay,
        CancellationToken cancellationToken);
}
