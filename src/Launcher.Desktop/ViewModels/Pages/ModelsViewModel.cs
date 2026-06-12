using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Models.Catalog;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class ModelsViewModel : ViewModelBase
{
    private readonly HuggingFaceModelClient _hfClient;

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

    public string Title => "Модели";
    public string Description =>
        "Локальные GGUF-модели и поиск на Hugging Face. Совет: берите динамические кванты " +
        "(UD-Q4_K_XL от Unsloth/Bartowski) — при том же размере качество выше, чем у обычного Q4_K_M.";

    public ModelsViewModel()
        : this(new HuggingFaceModelClient(new HttpClient { BaseAddress = new Uri("https://huggingface.co") }))
    {
    }

    public ModelsViewModel(HuggingFaceModelClient hfClient)
    {
        _hfClient = hfClient;
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
                LocalModels.Add(new LocalModelRow(
                    System.IO.Path.GetFileName(m.Path),
                    m.Family,
                    m.Quant ?? "—",
                    $"{m.SizeGb:0.0} ГБ",
                    m.Path));
            }

            LocalStatus = found.Count == 0
                ? "GGUF-модели не найдены. Скачайте модель ниже на Hugging Face."
                : $"Найдено моделей: {found.Count}.";
        }
        catch (Exception ex)
        {
            LocalStatus = "Не удалось просканировать папку: " + ex.Message;
        }
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

            foreach (var r in results)
            {
                SearchResults.Add(new HfModelRow(
                    r.Id,
                    $"↓ {r.Downloads:N0}   ♥ {r.Likes:N0}",
                    DescribeQuants(r.SiblingFiles ?? Array.Empty<string>()),
                    r.IsRuntimeCompatible));
            }

            SearchStatus = results.Count == 0
                ? "Ничего не найдено. Попробуйте другой запрос."
                : $"Найдено: {results.Count}. Выбирайте репозитории с пометкой GGUF.";
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
