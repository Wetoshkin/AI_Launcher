using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public sealed class RemoteGgufDownloadOptionRowViewModel(HuggingFaceGgufDownloadOption option)
{
    public string Label => option.Label;

    public string Quant => option.Quant ?? "quant?";

    public string FileCountText => option.TotalFiles switch
    {
        1 => "1 файл",
        >= 2 and <= 4 => $"{option.TotalFiles} файла",
        _ => $"{option.TotalFiles} файлов"
    };

    public string ModeText => option.IsSplit ? "split" : "single";

    public string PrimaryUrl => option.PrimaryFile.DownloadUrl;
}
