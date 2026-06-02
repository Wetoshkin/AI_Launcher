namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceSiblingFile(
    string FileName,
    long? SizeBytes = null)
{
    public string FormattedSize => HuggingFaceFileSizeFormatter.Format(SizeBytes);
}
