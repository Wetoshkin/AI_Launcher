namespace Launcher.Core.Decoding;

public sealed record DecodingPreset(
    string Id,
    string Name,
    string Description,
    bool EnableMtp,
    string? SpecType,
    bool IgnoreEos,
    LoopRiskLevel LoopRisk,
    IReadOnlyDictionary<string, string> Arguments);
