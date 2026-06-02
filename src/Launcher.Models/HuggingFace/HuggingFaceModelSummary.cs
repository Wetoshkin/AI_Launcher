namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceModelSummary(
    string Id,
    long Downloads,
    int Likes,
    IReadOnlyList<string> Tags,
    bool IsCompatibleWithCurrentGpu,
    bool HasPreferredQuant,
    bool IsRuntimeCompatible,
    IReadOnlyList<string>? SiblingFiles = null,
    IReadOnlyList<HuggingFaceSiblingFile>? SiblingFileMetadata = null);
