namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceModelDownloadResult(
    IReadOnlyList<string> DownloadedFiles,
    IReadOnlyList<string> SkippedFiles);
