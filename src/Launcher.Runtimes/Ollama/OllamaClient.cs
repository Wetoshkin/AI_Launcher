using System.Text.Json;

namespace Launcher.Runtimes.Ollama;

public sealed class OllamaClient(IOllamaHttpClient httpClient, Uri baseUri) : IOllamaRuntimeClient
{
    public async Task<IReadOnlyList<string>> ListTagsAsync(CancellationToken cancellationToken)
    {
        var json = await httpClient.GetStringAsync(BuildUri("/api/tags"), cancellationToken);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return models.EnumerateArray()
            .Select(item => GetString(item, "name"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();
    }

    public async Task<IReadOnlyList<string>> ListOpenAiModelsAsync(CancellationToken cancellationToken)
    {
        var json = await httpClient.GetStringAsync(BuildUri("/v1/models"), cancellationToken);
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return data.EnumerateArray()
            .Select(item => GetString(item, "id"))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToArray();
    }

    public async Task<bool> TinyGenerateAsync(string modelName, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            model = modelName,
            prompt = "ping",
            stream = false,
            options = new { num_predict = 1 }
        });
        var json = await httpClient.PostJsonAsync(BuildUri("/api/generate"), payload, cancellationToken);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("done", out var done) && done.ValueKind == JsonValueKind.True;
    }

    private Uri BuildUri(string path)
    {
        return new Uri(baseUri, path);
    }

    private static string? GetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
