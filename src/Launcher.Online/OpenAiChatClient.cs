using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Launcher.Online;

/// <summary>
/// Стриминговый клиент OpenAI-совместимого чата (/v1/chat/completions, stream=true).
/// Работает и с локальным llama-server, и с онлайн-провайдерами (через API-ключ).
/// </summary>
public sealed class OpenAiChatClient(HttpClient httpClient) : IChatClient
{
    public async IAsyncEnumerable<string> StreamAsync(
        ChatEndpoint endpoint,
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = endpoint.Model,
            stream = true,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
        };

        var url = endpoint.BaseUrl.TrimEnd('/') + "/chat/completions";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
        };
        if (!string.IsNullOrWhiteSpace(endpoint.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", endpoint.ApiKey);
        }

        using var response = await httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (!OpenAiStreamParser.TryParseLine(line, out var content, out var done))
            {
                continue;
            }

            if (done)
            {
                yield break;
            }

            if (!string.IsNullOrEmpty(content))
            {
                yield return content!;
            }
        }
    }
}
