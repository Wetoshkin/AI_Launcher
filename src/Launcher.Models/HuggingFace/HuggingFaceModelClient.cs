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
                    .ToArray() ?? []))
            .Where(model => !string.IsNullOrWhiteSpace(model.Id))
            .ToArray();
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

    private sealed record ApiModel(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("downloads")] long Downloads,
        [property: JsonPropertyName("likes")] int Likes,
        [property: JsonPropertyName("tags")] IReadOnlyList<string>? Tags,
        [property: JsonPropertyName("siblings")] IReadOnlyList<ApiSibling>? Siblings);

    private sealed record ApiSibling(
        [property: JsonPropertyName("rfilename")] string? Rfilename);
}
