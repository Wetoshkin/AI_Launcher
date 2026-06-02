namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceGgufFile(
    string FileName,
    string DownloadUrl,
    bool IsFirstSplitShard,
    long? SizeBytes = null)
{
    public string FormattedSize => HuggingFaceFileSizeFormatter.Format(SizeBytes);
}
