namespace Launcher.Core.Profiles;

public sealed record KvCacheSettings(
    string? TypeK,
    string? TypeV,
    bool FlashAttention,
    bool OffloadKqv);
