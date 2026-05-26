namespace Launcher.Runtimes.Ollama;

public interface IOllamaHttpClient
{
    Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken);

    Task<string> PostJsonAsync(Uri uri, string json, CancellationToken cancellationToken);
}
