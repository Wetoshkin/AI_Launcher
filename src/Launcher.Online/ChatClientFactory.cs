using System.Net;

namespace Launcher.Online;

/// <summary>Настройки прокси (по умолчанию — Hiddify). Локальные адреса всегда в обход.</summary>
public sealed record ProxySettings(bool Enabled, string Address)
{
    public static ProxySettings Hiddify { get; } = new(false, "http://127.0.0.1:12334");
}

/// <summary>
/// Создаёт подходящий <see cref="IChatClient"/> под провайдера и прокси.
/// Для Anthropic — AnthropicChatClient, иначе OpenAI-совместимый клиент.
/// </summary>
public static class ChatClientFactory
{
    public static IChatClient Create(ProviderPreset preset, ProxySettings? proxy = null)
    {
        var isLocal = preset.Kind == ProviderKind.Local || IsLoopback(preset.BaseUrl);
        var http = BuildHttpClient(proxy, isLocal);
        return preset.IsAnthropic ? new AnthropicChatClient(http) : new OpenAiChatClient(http);
    }

    public static HttpClient BuildHttpClient(ProxySettings? proxy, bool bypassForLocal)
    {
        var handler = new HttpClientHandler();
        if (proxy is { Enabled: true } && !bypassForLocal && !string.IsNullOrWhiteSpace(proxy.Address))
        {
            handler.Proxy = new WebProxy(proxy.Address)
            {
                BypassProxyOnLocal = true,
                BypassList = ["localhost", "127.0.0.1", "::1"],
            };
            handler.UseProxy = true;
        }
        else
        {
            handler.UseProxy = false;
        }

        return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };
    }

    private static bool IsLoopback(string baseUrl) =>
        Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
