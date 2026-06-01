namespace Launcher.Runtimes.Processes;

public sealed record ProcessStartRequest(
    string Executable,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string> Environment,
    string? WorkingDirectory,
    Action<string>? OutputReceived = null);
