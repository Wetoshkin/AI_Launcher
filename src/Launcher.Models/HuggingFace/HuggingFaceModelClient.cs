using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Launcher.Models.HuggingFace;

public sealed class HuggingFaceModelClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<HuggingFaceModelSummary>> SearchAsync(
        HuggingFaceModelSearchRequest request,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["search"] = request.Query,
            ["sort"] = SortName(request.Sort),
            ["direction"] = "-1",
            ["limit"] = Math.Clamp(request.Limit, 1, 100).ToString(),
            ["full"] = "true"
        };

        if (request.GgufOnly)
        {
            query["filter"] = "gguf";
        }

        var uri = "/api/models?" + string.Join("&", query
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value!)}"));
        var response = await httpClient.GetFromJsonAsync<List<ApiModel>>(uri, cancellationToken)
            ?? [];

        return response.Select(model => new HuggingFaceModelSummary(
                model.Id ?? "",
                model.Downloads,
                model.Likes,
                model.Tags ?? [],
                IsCompatibleWithCurrentGpu: false,
                HasPreferredQuant: HasPreferredQuant(model),
                IsRuntimeCompatible: IsGgufCompatible(model),
                SiblingFiles: model.Siblings?.Select(sibling => sibling.Rfilename ?? "")
                    .Where(file => !string.IsNullOrWhiteSpace(file))
                    .ToArray() ?? [],
                SiblingFileMetadata: model.Siblings?
                    .Where(sibling => !string.IsNullOrWhiteSpace(sibling.Rfilename))
                    .Select(sibling => new HuggingFaceSiblingFile(
                        sibling.Rfilename!,
                        NormalizeSize(sibling.SizeBytes ?? sibling.Size ?? sibling.Lfs?.Size)))
                    .ToArray() ?? []))
            .Where(model => !string.IsNullOrWhiteSpace(model.Id))
            .ToArray();
    }

    /// <summary>
    /// Возвращает список GGUF-файлов репозитория с реальными размерами (из дерева файлов).
    /// Поиск таких размеров не отдаёт, поэтому для оценки «влезет/не влезет» нужен отдельный запрос.
    /// </summary>
    public async Task<IReadOnlyList<HuggingFaceSiblingFile>> GetGgufFilesAsync(
        string repoId,
        CancellationToken cancellationToken)
    {
        var escaped = string.Join("/", repoId
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));
        var uri = $"/api/models/{escaped}/tree/main?recursive=true";

        var entries = await httpClient.GetFromJsonAsync<List<ApiTreeEntry>>(uri, cancellationToken) ?? [];

        return entries
            .Where(e => e.Type == "file"
                && e.Path is not null
                && e.Path.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            .Select(e => new HuggingFaceSiblingFile(e.Path!, NormalizeSize(e.Lfs?.Size ?? e.Size)))
            .ToArray();
    }

    private sealed record ApiTreeEntry(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("size")] long? Size,
        [property: JsonPropertyName("lfs")] ApiLfs? Lfs);

    /// <summary>Загружает README модели (markdown). Возвращает null, если недоступен.</summary>
    public async Task<string?> GetReadmeAsync(string repoId, CancellationToken cancellationToken)
    {
        var escaped = string.Join("/", repoId
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));

        try
        {
            return await httpClient.GetStringAsync($"/{escaped}/raw/main/README.md", cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static string SortName(HuggingFaceSort sort) => sort switch
    {
        HuggingFaceSort.Downloads => "downloads",
        HuggingFaceSort.Likes => "likes",
        HuggingFaceSort.CreatedAt => "createdAt",
        HuggingFaceSort.LastModified => "lastModified",
        _ => "trendingScore"
    };

    private static bool IsGgufCompatible(ApiModel model) =>
        model.Tags?.Any(tag => tag.Equals("gguf", StringComparison.OrdinalIgnoreCase)) == true
        || model.Siblings?.Any(sibling => sibling.Rfilename?.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) == true) == true;

    private static bool HasPreferredQuant(ApiModel model) =>
        model.Siblings?.Any(sibling =>
            sibling.Rfilename?.Contains("Q4_K_M", StringComparison.OrdinalIgnoreCase) == true
            || sibling.Rfilename?.Contains("Q5_K_M", StringComparison.OrdinalIgnoreCase) == true
            || sibling.Rfilename?.Contains("Q6_K", StringComparison.OrdinalIgnoreCase) == true) == true;

    private static long? NormalizeSize(long? sizeBytes) =>
        sizeBytes is > 0 ? sizeBytes : null;

    private sealed record ApiModel(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("downloads")] long Downloads,
        [property: JsonPropertyName("likes")] int Likes,
        [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
        [property: JsonPropertyName("siblings")] IReadOnlyList<ApiSibling>? Siblings);

    private sealed record ApiSibling(
        [property: JsonPropertyName("rfilename")] string? Rfilename,
        [property: JsonPropertyName("size")] long? Size,
        [property: JsonPropertyName("sizeBytes")] long? SizeBytes,
        [property: JsonPropertyName("lfs")] ApiLfs? Lfs);

    private sealed record ApiLfs(
        [property: JsonPropertyName("size")] long? Size);
}
