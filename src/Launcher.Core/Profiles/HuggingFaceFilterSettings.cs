namespace Launcher.Core.Profiles;

public sealed record HuggingFaceFilterSettings(
    string? SearchQuery,
    string? Author,
    string? Quantization,
    string? Architecture,
    string? Task,
    string? Sort,
    bool ShowGated,
    bool ShowIncompatible);
