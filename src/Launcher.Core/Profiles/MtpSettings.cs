namespace Launcher.Core.Profiles;

public sealed record MtpSettings(
    bool Enabled,
    string? DraftModelPath,
    int? DraftTokens,
    string? SpeculativeType);
