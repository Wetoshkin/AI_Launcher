namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceGgufDownloadOption(
    string Label,
    string? Quant,
    bool IsSplit,
    IReadOnlyList<HuggingFaceGgufFile> Files)
{
    public int TotalFiles => Files.Count;

    public HuggingFaceGgufFile PrimaryFile => Files.First();

    public long? TotalSizeBytes => Files.All(file => file.SizeBytes.HasValue)
        ? Files.Sum(file => file.SizeBytes!.Value)
        : null;

    public string FormattedSize => HuggingFaceFileSizeFormatter.Format(TotalSizeBytes);
}
