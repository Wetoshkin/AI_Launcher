namespace Launcher.Runtimes.Startup;

public sealed record EndpointHealthResult(
    bool IsReady,
    int Attempts,
    string Message);
