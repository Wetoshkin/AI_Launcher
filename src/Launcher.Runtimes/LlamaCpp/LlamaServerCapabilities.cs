namespace Launcher.Runtimes.LlamaCpp;

public sealed record LlamaServerCapabilities(
    IReadOnlySet<string> Flags,
    IReadOnlySet<string> CacheTypes,
    IReadOnlySet<string> SpecTypes,
    bool SupportsTurboQuant,
    bool SupportsMtp)
{
    public bool SupportsFlag(string flag) => Flags.Contains(flag);
}
