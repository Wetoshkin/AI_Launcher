namespace Launcher.Runtimes.Startup;

public sealed class OpenAiEndpointHealthClient(HttpClient httpClient) : IEndpointHealthClient
{
    public async Task<EndpointHealthResult> WaitUntilReadyAsync(
        string baseUrl,
        int Attempts,
        TimeSpan Delay,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, Attempts);
        var modelsUri = BuildModelsUri(baseUrl);
        string lastMessage = "endpoint не ответил";

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(modelsUri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return new EndpointHealthResult(true, attempt, "endpoint готов");
                }

                lastMessage = $"endpoint вернул HTTP {(int)response.StatusCode}";
            }
            catch (HttpRequestException ex)
            {
                lastMessage = ex.Message;
            }

            if (attempt < attempts && Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }
        }

        return new EndpointHealthResult(false, attempts, lastMessage);
    }

    private static Uri BuildModelsUri(string baseUrl)
    {
        var normalized = baseUrl.TrimEnd('/');
        return new Uri($"{normalized}/models", UriKind.Absolute);
    }
}
