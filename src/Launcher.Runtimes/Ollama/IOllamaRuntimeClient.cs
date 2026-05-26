namespace Launcher.Runtimes.Ollama;

public interface IOllamaRuntimeClient
{
    Task<IReadOnlyList<string>> ListTagsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListOpenAiModelsAsync(CancellationToken cancellationToken);

    Task<bool> TinyGenerateAsync(string modelName, CancellationToken cancellationToken);
}
