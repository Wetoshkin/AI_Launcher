namespace Launcher.Runtimes.Memory;

public sealed record ModelMemorySpec(
    double SizeGb,
    double ParametersBillion,
    int? NativeContextTokens);
