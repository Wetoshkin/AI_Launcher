using System.Text;

namespace Launcher.Runtimes.Ollama;

public sealed class OllamaHttpClient(HttpClient httpClient) : IOllamaHttpClient
{
    public async Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> PostJsonAsync(Uri uri, string json, CancellationToken cancellationToken)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync(uri, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
