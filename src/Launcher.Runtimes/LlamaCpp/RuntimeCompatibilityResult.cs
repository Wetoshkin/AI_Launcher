namespace Launcher.Runtimes.LlamaCpp;

public sealed record RuntimeCompatibilityResult(
    bool IsCompatible,
    IReadOnlyList<string> Messages);
