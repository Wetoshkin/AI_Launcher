namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceGgufDownloadOption(
    string Label,
    string? Quant,
    bool IsSplit,
    IReadOnlyList<HuggingFaceGgufFile> Files)
{
    public int TotalFiles => Files.Count;

    public HuggingFaceGgufFile PrimaryFile => Files.First();
}
