namespace Launcher.Runtimes.Startup;

public sealed record RuntimeStartResult(
    bool Started,
    int? ProcessId,
    IReadOnlyList<string> Messages);
