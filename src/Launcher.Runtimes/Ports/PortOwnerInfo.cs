namespace Launcher.Runtimes.Ports;

public sealed record PortOwnerInfo(
    int Port,
    int ProcessId,
    string ProcessName,
    string? ExecutablePath,
    bool EndpointResponds,
    string? LoadedModelId)
{
    public bool IsLikelyLlamaServer =>
        ProcessName.Contains("llama-server", StringComparison.OrdinalIgnoreCase);
}
