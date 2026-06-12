namespace Launcher.Online;

public enum ProviderKind
{
    Local,
    OpenAi,
    OpenRouter,
    Anthropic,
    Custom
}

/// <summary>
/// Пресет провайдера модели: понятное имя, адрес API, модель по умолчанию,
/// нужен ли ключ и какой протокол (OpenAI-совместимый или Anthropic).
/// </summary>
public sealed record ProviderPreset(
    ProviderKind Kind,
    string DisplayName,
    string BaseUrl,
    string DefaultModel,
    bool RequiresKey,
    bool IsAnthropic = false);

public static class ProviderRegistry
{
    public static IReadOnlyList<ProviderPreset> All { get; } = new[]
    {
        new ProviderPreset(ProviderKind.Local, "Локальный сервер", "http://127.0.0.1:8080/v1", "local-model", RequiresKey: false),
        new ProviderPreset(ProviderKind.OpenAi, "OpenAI", "https://api.openai.com/v1", "gpt-4o-mini", RequiresKey: true),
        new ProviderPreset(ProviderKind.OpenRouter, "OpenRouter", "https://openrouter.ai/api/v1", "openai/gpt-4o-mini", RequiresKey: true),
        new ProviderPreset(ProviderKind.Anthropic, "Anthropic (Claude)", "https://api.anthropic.com/v1", "claude-3-5-haiku-latest", RequiresKey: true, IsAnthropic: true),
        new ProviderPreset(ProviderKind.Custom, "Свой адрес", "http://127.0.0.1:8080/v1", "local-model", RequiresKey: false),
    };

    public static ProviderPreset Default => All[0];

    public static ProviderPreset ForKind(ProviderKind kind) =>
        All.FirstOrDefault(p => p.Kind == kind) ?? Default;
}
