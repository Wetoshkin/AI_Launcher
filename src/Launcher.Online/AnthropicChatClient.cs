using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Launcher.Online;

/// <summary>
/// Стриминговый клиент Anthropic Messages API (Claude). Системные сообщения выносятся в поле
/// system, остальные — в messages. Авторизация через заголовок x-api-key.
/// </summary>
public sealed class AnthropicChatClient(HttpClient httpClient) : IChatClient
{
    private const int DefaultMaxTokens = 1024;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public async IAsyncEnumerable<string> StreamAsync(
        ChatEndpoint endpoint,
        IReadOnlyList<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var system = string.Join("\n", messages
            .Where(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Content));

        var dialog = messages
            .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
            .Select(m => new { role = m.Role, content = m.Content })
            .ToArray();

        var payload = new Dictionary<string, object?>
        {
            ["model"] = endpoint.Model,
            ["max_tokens"] = DefaultMaxTokens,
            ["stream"] = true,
            ["messages"] = dialog,
        };
        if (!string.IsNullOrWhiteSpace(system))
        {
            payload["system"] = system;
        }

        var url = endpoint.BaseUrl.TrimEnd('/') + "/messages";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-api-key", endpoint.ApiKey ?? string.Empty);
        request.Headers.Add("anthropic-version", "2023-06-01");

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

            if (!AnthropicStreamParser.TryParseLine(line, out var content, out var done))
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
