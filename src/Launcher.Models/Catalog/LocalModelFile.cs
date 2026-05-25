namespace Launcher.Models.Catalog;

public sealed record LocalModelFile(
    string Path,
    string Family,
    string? SizeLabel,
    string? Quant,
    double SizeGb);
