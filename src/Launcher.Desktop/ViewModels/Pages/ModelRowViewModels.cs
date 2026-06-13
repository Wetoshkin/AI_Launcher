using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>Строка локальной модели в каталоге.</summary>
public sealed record LocalModelRow(
    string FileName,
    string Family,
    string Quant,
    string Size,
    string Path,
    string Fit,
    int FitLevel);

/// <summary>Строка результата поиска на Hugging Face. Размер и пригодность подгружаются асинхронно.</summary>
public sealed partial class HfModelRow : ObservableObject
{
    public string Id { get; }
    public string Stats { get; }
    public string Quants { get; }
    public bool HasGguf { get; }

    /// <summary>Рекомендованный квант для скачивания (подобран под железо).</summary>
    public string? RecommendedQuant { get; set; }

    [ObservableProperty]
    private string _fit = "Оцениваю размер…";

    [ObservableProperty]
    private int _fitLevel = 3;

    [ObservableProperty]
    private bool _canDownload;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _downloadStatus = string.Empty;

    public HfModelRow(string id, string stats, string quants, bool hasGguf)
    {
        Id = id;
        Stats = stats;
        Quants = quants;
        HasGguf = hasGguf;
    }
}
