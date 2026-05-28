namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceGgufFile(
    string FileName,
    string DownloadUrl,
    bool IsFirstSplitShard);
