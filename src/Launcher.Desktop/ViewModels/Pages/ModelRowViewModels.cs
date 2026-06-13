using CommunityToolkit.Mvvm.ComponentModel;

namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>Строка локальной модели в каталоге. Контекст и возможности подгружаются асинхронно.</summary>
public sealed partial class LocalModelRow : ObservableObject
{
    public string FileName { get; }
    public string Family { get; }
    public string Quant { get; }
    public string Size { get; }
    public string Path { get; }
    public double SizeGb { get; }

    [ObservableProperty]
    private string _fit;

    [ObservableProperty]
    private int _fitLevel;

    [ObservableProperty]
    private string _context = string.Empty;

    [ObservableProperty]
    private int _contextValue;

    [ObservableProperty]
    private string _caps = string.Empty;

    public LocalModelRow(string fileName, string family, string quant, string size, string path, double sizeGb, string fit, int fitLevel)
    {
        FileName = fileName;
        Family = family;
        Quant = quant;
        Size = size;
        Path = path;
        SizeGb = sizeGb;
        _fit = fit;
        _fitLevel = fitLevel;
    }
}

/// <summary>Строка результата поиска на Hugging Face. Размер и пригодность подгружаются асинхронно.</summary>
public sealed partial class HfModelRow : ObservableObject
{
    public string Id { get; }
    public string Stats { get; }
    public string Quants { get; }
    public bool HasGguf { get; }

    /// <summary>Рекомендованный квант для скачивания (подобран под железо).</summary>
    public string? RecommendedQuant { get; set; }

    public bool HasTools { get; set; }
    public bool HasVision { get; set; }

    [ObservableProperty]
    private string _caps = string.Empty;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private bool _visible = true;

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
