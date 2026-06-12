namespace Launcher.Online;

/// <summary>Одно сообщение чата. Role: "system" | "user" | "assistant".</summary>
public sealed record ChatMessage(string Role, string Content);

/// <summary>
/// Куда слать чат: базовый URL OpenAI-совместимого API (например http://127.0.0.1:8080/v1),
/// id модели и опциональный API-ключ (для онлайн-провайдеров).
/// </summary>
public sealed record ChatEndpoint(string BaseUrl, string Model, string? ApiKey = null);

public interface IChatClient
{
    IAsyncEnumerable<string> StreamAsync(
        ChatEndpoint endpoint,
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken);
}
