namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceDownloadProgress(
    string FileName,
    int FileIndex,
    int TotalFiles,
    long BytesReceived,
    long? TotalBytes,
    bool IsSkipped);
