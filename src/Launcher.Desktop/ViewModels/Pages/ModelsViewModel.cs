using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Services;
using Launcher.Models.Catalog;
using Launcher.Models.HuggingFace;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class ModelsViewModel : ViewModelBase
{
    private readonly HuggingFaceModelClient _hfClient;
    private readonly IHuggingFaceModelDownloadService _downloadService;
    private readonly ConcurrentDictionary<string, IReadOnlyList<HuggingFaceSiblingFile>> _fileCache = new();

    private SystemHardware? _hardware;
    private double _vramGb;
    private double _ramGb;

    [ObservableProperty]
    private string _modelsFolder = string.Empty;

    [ObservableProperty]
    private string _localStatus = "Укажите папку с моделями и нажмите «Сканировать».";

    [ObservableProperty]
    private string _searchQuery = "qwen";

    [ObservableProperty]
    private string _searchStatus = "Поиск GGUF-моделей на Hugging Face.";

    [ObservableProperty]
    private bool _isSearching;

    public ObservableCollection<LocalModelRow> LocalModels { get; } = new();
    public ObservableCollection<HfModelRow> SearchResults { get; } = new();

    /// <summary>Делегат выбора папки (подставляет App с доступом к окну).</summary>
    public Func<string, Task<string?>>? PickFolderAsync { get; set; }

    /// <summary>Передать выбранную локальную модель в «Чат» (путь к GGUF).</summary>
    public Action<string>? UseLocalModel { get; set; }

    public string Title => "Модели";
    public string Description =>
        "Локальные GGUF-модели и поиск на Hugging Face. Совет: берите динамические кванты " +
        "(UD-Q4_K_XL от Unsloth/Bartowski) — при том же размере качество выше, чем у обычного Q4_K_M.";

    public ModelsViewModel()
        : this(
            new HuggingFaceModelClient(new HttpClient { BaseAddress = new Uri("https://huggingface.co") }),
            new HuggingFaceModelDownloadService(new HttpClient { BaseAddress = new Uri("https://huggingface.co") }))
    {
    }

    public ModelsViewModel(HuggingFaceModelClient hfClient)
        : this(hfClient, new HuggingFaceModelDownloadService(new HttpClient { BaseAddress = new Uri("https://huggingface.co") }))
    {
    }

    public ModelsViewModel(HuggingFaceModelClient hfClient, IHuggingFaceModelDownloadService downloadService)
    {
        _hfClient = hfClient;
        _downloadService = downloadService;
        GpuSettings.Instance.Changed += (_, _) => Dispatcher.UIThread.Post(RecomputeBudget);
    }

    public void ApplyHardware(SystemHardware hardware)
    {
        _hardware = hardware;
        RecomputeBudget();
    }

    private void RecomputeBudget()
    {
        if (_hardware is null)
        {
            return;
        }

        _ramGb = _hardware.RamTotalGb;
        _vramGb = GpuClassifier.UsableVramGb(_hardware, GpuSettings.Instance.UseIntegratedGpu);

        // Пересчитать бейджи у уже показанных строк.
        RecomputeLocalFit();
    }

    private void RecomputeLocalFit()
    {
        if (LocalModels.Count == 0)
        {
            return;
        }

        var snapshot = LocalModels.ToList();
        LocalModels.Clear();
        foreach (var m in snapshot)
        {
            var sizeGb = ParseGb(m.Size);
            var (fit, level) = ModelFit.Describe(sizeGb, _vramGb, _ramGb);
            LocalModels.Add(m with { Fit = fit, FitLevel = level });
        }
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        if (PickFolderAsync is null)
        {
            return;
        }

        var folder = await PickFolderAsync("Папка с GGUF-моделями");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            ModelsFolder = folder;
            Scan();
        }
    }

    [RelayCommand]
    private void Scan()
    {
        LocalModels.Clear();
        if (string.IsNullOrWhiteSpace(ModelsFolder))
        {
            LocalStatus = "Сначала выберите папку с моделями.";
            return;
        }

        try
        {
            var found = LocalModelCatalog.Scan(new[] { ModelsFolder });
            foreach (var m in found)
            {
                var (fit, level) = ModelFit.Describe(m.SizeGb, _vramGb, _ramGb);
                LocalModels.Add(new LocalModelRow(
                    System.IO.Path.GetFileName(m.Path),
                    m.Family,
                    m.Quant ?? "—",
                    $"{m.SizeGb:0.0} ГБ",
                    m.Path,
                    fit,
                    level));
            }

            LocalStatus = found.Count == 0
                ? "GGUF-модели не найдены. Скачайте модель ниже на Hugging Face."
                : $"Найдено моделей: {found.Count}. Нажмите «Использовать», чтобы открыть модель в Чате.";
        }
        catch (Exception ex)
        {
            LocalStatus = "Не удалось просканировать папку: " + ex.Message;
        }
    }

    [RelayCommand]
    private void UseLocal(LocalModelRow? row)
    {
        if (row is null)
        {
            return;
        }

        UseLocalModel?.Invoke(row.Path);
        LocalStatus = $"Модель «{row.FileName}» открыта в Чате. Перейдите на вкладку 💬 и запустите сервер.";
    }

    private bool CanSearch => !IsSearching && !string.IsNullOrWhiteSpace(SearchQuery);

    partial void OnIsSearchingChanged(bool value) => SearchHuggingFaceCommand.NotifyCanExecuteChanged();
    partial void OnSearchQueryChanged(string value) => SearchHuggingFaceCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private async Task SearchHuggingFaceAsync()
    {
        IsSearching = true;
        SearchStatus = "Ищем на Hugging Face…";
        SearchResults.Clear();

        try
        {
            var request = new HuggingFaceModelSearchRequest(SearchQuery.Trim(), HuggingFaceSort.Downloads, Limit: 20);
            var results = await _hfClient.SearchAsync(request, CancellationToken.None);

            var rows = new List<HfModelRow>();
            foreach (var r in results)
            {
                var row = new HfModelRow(
                    r.Id,
                    $"↓ {r.Downloads:N0}   ♥ {r.Likes:N0}",
                    DescribeQuants(r.SiblingFiles ?? Array.Empty<string>()),
                    r.IsRuntimeCompatible);
                rows.Add(row);
                SearchResults.Add(row);
            }

            SearchStatus = results.Count == 0
                ? "Ничего не найдено. Попробуйте другой запрос."
                : $"Найдено: {results.Count}. Размер и пригодность под ваше железо подгружаются…";

            _ = FillSizesAsync(rows.Where(r => r.HasGguf).ToList());
        }
        catch (Exception ex)
        {
            SearchStatus = "Ошибка поиска: " + ex.Message + ". Если провайдер заблокирован — включите прокси Hiddify в системе.";
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>Подгружает реальные размеры файлов и считает «влезет ли», параллельно но без перегруза.</summary>
    private async Task FillSizesAsync(IReadOnlyList<HfModelRow> rows)
    {
        using var gate = new SemaphoreSlim(5);

        var tasks = rows.Select(async row =>
        {
            await gate.WaitAsync();
            try
            {
                var option = await GetRecommendedOptionAsync(row.Id, CancellationToken.None);
                var sizeGb = option?.TotalSizeBytes is long b ? b / 1024.0 / 1024.0 / 1024.0 : 0;
                var (fit, level) = ModelFit.Describe(sizeGb, _vramGb, _ramGb);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (option is null)
                    {
                        row.Fit = "Не удалось определить размер";
                        row.FitLevel = 3;
                        return;
                    }

                    row.RecommendedQuant = option.Quant ?? option.Label;
                    row.Fit = $"{fit}  ·  квант {row.RecommendedQuant}";
                    row.FitLevel = level;
                    row.CanDownload = true;
                });
            }
            catch
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    row.Fit = "Не удалось определить размер";
                    row.FitLevel = 3;
                });
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        await Dispatcher.UIThread.InvokeAsync(() =>
            SearchStatus = SearchResults.Count == 0 ? SearchStatus : $"Найдено: {SearchResults.Count}. Зелёный бейдж — модель поместится в видеопамять.");
    }

    [RelayCommand]
    private async Task DownloadHfAsync(HfModelRow? row)
    {
        if (row is null)
        {
            return;
        }

        // Папка общая с разделом «Локальные модели». Если её ещё нет — предложим выбрать прямо здесь.
        if (string.IsNullOrWhiteSpace(ModelsFolder) && PickFolderAsync is not null)
        {
            var folder = await PickFolderAsync("Куда скачивать модели");
            if (!string.IsNullOrWhiteSpace(folder))
            {
                ModelsFolder = folder;
            }
        }

        if (string.IsNullOrWhiteSpace(ModelsFolder))
        {
            row.DownloadStatus = "Выберите папку для моделей (поле «Локальные модели» вверху) — туда сохраним файл.";
            return;
        }

        row.IsBusy = true;
        row.DownloadProgress = 0;
        row.DownloadStatus = "Подбираю файл…";

        try
        {
            var option = await GetRecommendedOptionAsync(row.Id, CancellationToken.None);
            if (option is null)
            {
                row.DownloadStatus = "В репозитории не нашлось подходящего GGUF-файла.";
                return;
            }

            var label = option.Quant ?? option.Label;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var lastTick = 0.0;
            long lastBytes = 0;
            var speedText = string.Empty;

            // Каждая загрузка идёт в своей задаче и обновляет только свою строку —
            // поэтому несколько моделей качаются параллельно, не мешая друг другу.
            var request = new HuggingFaceModelDownloadRequest(row.Id, option, ModelsFolder);
            await _downloadService.DownloadAsync(request, CancellationToken.None, p =>
            {
                if (p.IsSkipped)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        row.DownloadProgress = 100;
                        row.DownloadStatus = $"Уже скачано: {p.FileName}";
                    });
                    return;
                }

                var pct = p.TotalBytes is > 0 ? 100.0 * p.BytesReceived / p.TotalBytes.Value : 0;

                var elapsed = sw.Elapsed.TotalSeconds;
                if (elapsed - lastTick >= 0.5)
                {
                    var bytesPerSec = (p.BytesReceived - lastBytes) / System.Math.Max(0.001, elapsed - lastTick);
                    speedText = FormatSpeed(bytesPerSec);
                    lastBytes = p.BytesReceived;
                    lastTick = elapsed;

                    var received = FormatBytes(p.BytesReceived);
                    var total = p.TotalBytes is > 0 ? FormatBytes(p.TotalBytes.Value) : "?";
                    var fileInfo = p.TotalFiles > 1 ? $" · файл {p.FileIndex}/{p.TotalFiles}" : string.Empty;

                    Dispatcher.UIThread.Post(() =>
                    {
                        row.DownloadProgress = pct;
                        row.DownloadStatus = $"Скачиваю {label}: {pct:0}% · {received} из {total} · {speedText}{fileInfo}";
                    });
                }
                else
                {
                    Dispatcher.UIThread.Post(() => row.DownloadProgress = pct);
                }
            });

            row.DownloadProgress = 100;
            row.DownloadStatus = "Готово ✓ — модель добавлена в папку.";
            Scan();
        }
        catch (Exception ex)
        {
            row.DownloadStatus = "Ошибка загрузки: " + ex.Message;
        }
        finally
        {
            row.IsBusy = false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        double b = bytes;
        if (b >= 1024L * 1024 * 1024)
        {
            return $"{b / 1024 / 1024 / 1024:0.0} ГБ";
        }

        return $"{b / 1024 / 1024:0} МБ";
    }

    private static string FormatSpeed(double bytesPerSec)
    {
        if (bytesPerSec >= 1024.0 * 1024)
        {
            return $"{bytesPerSec / 1024 / 1024:0.0} МБ/с";
        }

        return $"{bytesPerSec / 1024:0} КБ/с";
    }

    /// <summary>Берёт файлы репозитория и выбирает лучший квант, который влезает в железо.</summary>
    private async Task<HuggingFaceGgufDownloadOption?> GetRecommendedOptionAsync(string repoId, CancellationToken ct)
    {
        if (!_fileCache.TryGetValue(repoId, out var files))
        {
            files = await _hfClient.GetGgufFilesAsync(repoId, ct);
            _fileCache[repoId] = files;
        }

        var summary = new HuggingFaceModelSummary(repoId, 0, 0, Array.Empty<string>(),
            false, false, true, SiblingFileMetadata: files);
        var options = HuggingFaceGgufFileSelector.SelectDownloadOptions(summary)
            .Where(o => o.TotalSizeBytes is > 0)
            .ToList();

        if (options.Count == 0)
        {
            return null;
        }

        double Gb(HuggingFaceGgufDownloadOption o) => (o.TotalSizeBytes ?? 0) / 1024.0 / 1024.0 / 1024.0;

        // Лучшее качество, что целиком влезет в видеопамять.
        var fitsVram = options
            .Where(o => _vramGb > 0 && Gb(o) + 1.5 <= _vramGb)
            .OrderByDescending(Gb)
            .FirstOrDefault();
        if (fitsVram is not null)
        {
            return fitsVram;
        }

        // Иначе — лучшее, что влезет в ОЗУ (+VRAM с offload).
        var fitsRam = options
            .Where(o => Gb(o) + 3 <= _ramGb + Math.Max(0, _vramGb))
            .OrderByDescending(Gb)
            .FirstOrDefault();
        if (fitsRam is not null)
        {
            return fitsRam;
        }

        // На крайний случай — самый маленький.
        return options.OrderBy(Gb).First();
    }

    private static double ParseGb(string size)
    {
        var digits = new string(size.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray()).Replace(',', '.');
        return double.TryParse(digits, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;
    }

    private static string DescribeQuants(IReadOnlyList<string> files)
    {
        var quants = files
            .Where(f => f.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            .Select(ExtractQuant)
            .Where(q => q is not null)
            .Distinct()
            .Take(6)
            .ToArray();

        return quants.Length == 0 ? "—" : string.Join(", ", quants);
    }

    private static string? ExtractQuant(string fileName)
    {
        var markers = new[] { "UD-Q4_K_XL", "UD-Q2_K_XL", "Q4_K_M", "Q5_K_M", "Q6_K", "Q8_0", "Q4_0", "Q3_K" };
        return markers.FirstOrDefault(m => fileName.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
