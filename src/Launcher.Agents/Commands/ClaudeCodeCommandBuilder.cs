using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

/// <summary>
/// Claude Code (Anthropic) запускается в папке проекта и общается с локальным llama-server
/// через нативный Anthropic Messages API. Настраивается переменными окружения:
/// базовый адрес БЕЗ /v1, токен-заглушка и маппинг уровней моделей (opus/sonnet/haiku).
/// Запускается с --dangerously-skip-permissions: локальная модель работает без подтверждений на каждое действие.
/// </summary>
public sealed class ClaudeCodeCommandBuilder : IAgentCommandBuilder
{
    /// <summary>
    /// Настоящий ID флагманской модели Anthropic. Claude Code определяет «уровень», подпись в TUI
    /// и часть клиентского поведения по этому ID — подсовываем его, чтобы CLI считала, что работает
    /// с Opus 4.8. На деле запрос уходит на локальный llama-server, который игнорирует поле model
    /// в /v1/messages и всегда отдаёт загруженную GGUF. Спуфинг применяется ТОЛЬКО к Claude Code;
    /// остальные агенты получают реальное имя локальной модели.
    /// </summary>
    private const string SpoofedModel = "claude-opus-4-8";

    public LaunchPlan Build(AgentLaunchRequest request)
    {
        return new LaunchPlan(
            "claude",
            new[] { "--dangerously-skip-permissions" },
            new Dictionary<string, string>
            {
                ["ANTHROPIC_BASE_URL"] = StripV1(request.BaseUrl),
                ["ANTHROPIC_AUTH_TOKEN"] = "local",
                ["ANTHROPIC_DEFAULT_OPUS_MODEL"] = SpoofedModel,
                ["ANTHROPIC_DEFAULT_SONNET_MODEL"] = SpoofedModel,
                ["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = SpoofedModel,
                ["ANTHROPIC_MODEL"] = SpoofedModel
            });
    }

    /// <summary>Claude Code сам добавляет /v1/messages — базовый адрес должен быть «голым».</summary>
    private static string StripV1(string baseUrl)
    {
        var url = baseUrl.TrimEnd('/');
        return url.EndsWith("/v1", System.StringComparison.OrdinalIgnoreCase)
            ? url[..^3].TrimEnd('/')
            : url;
    }
}
