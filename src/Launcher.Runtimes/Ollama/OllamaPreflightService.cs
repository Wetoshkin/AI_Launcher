namespace Launcher.Runtimes.Ollama;

public sealed class OllamaPreflightService(IOllamaRuntimeClient client)
{
    public async Task<RuntimePreflightResult> CheckAsync(string modelName, CancellationToken cancellationToken)
    {
        var checks = new List<RuntimeCheck>();

        var tags = await client.ListTagsAsync(cancellationToken);
        var hasTag = tags.Contains(modelName, StringComparer.OrdinalIgnoreCase);
        checks.Add(new RuntimeCheck(
            "Ollama /api/tags",
            hasTag,
            hasTag ? "Модель видна в Ollama /api/tags." : $"Модель {modelName} не найдена в Ollama /api/tags."));

        var openAiModels = await client.ListOpenAiModelsAsync(cancellationToken);
        var hasOpenAiModel = openAiModels.Contains(modelName, StringComparer.OrdinalIgnoreCase);
        checks.Add(new RuntimeCheck(
            "Ollama /v1/models",
            hasOpenAiModel,
            hasOpenAiModel ? "Модель видна в OpenAI-compatible /v1/models." : $"Модель {modelName} не найдена в Ollama /v1/models."));

        var generateOk = await client.TinyGenerateAsync(modelName, cancellationToken);
        checks.Add(new RuntimeCheck(
            "Ollama /api/generate",
            generateOk,
            generateOk ? "Tiny generate preflight прошел." : "Tiny generate preflight не смог загрузить модель."));

        return new RuntimePreflightResult(checks);
    }
}
