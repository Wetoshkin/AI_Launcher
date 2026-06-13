using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

/// <summary>
/// Claude Code (Anthropic) запускается в папке проекта и общается с локальным llama-server
/// через нативный Anthropic Messages API. Настраивается переменными окружения:
/// базовый адрес БЕЗ /v1, токен-заглушка и маппинг уровней моделей (opus/sonnet/haiku) на нашу модель.
/// </summary>
public sealed class ClaudeCodeCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        var model = StripLocalPrefix(request.ProviderModel);

        return new LaunchPlan(
            "claude",
            System.Array.Empty<string>(),
            new Dictionary<string, string>
            {
                ["ANTHROPIC_BASE_URL"] = StripV1(request.BaseUrl),
                ["ANTHROPIC_AUTH_TOKEN"] = "local",
                ["ANTHROPIC_DEFAULT_OPUS_MODEL"] = model,
                ["ANTHROPIC_DEFAULT_SONNET_MODEL"] = model,
                ["ANTHROPIC_DEFAULT_HAIKU_MODEL"] = model,
                ["ANTHROPIC_MODEL"] = model
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

    private static string StripLocalPrefix(string model) =>
        model.StartsWith("local/", System.StringComparison.Ordinal) ? model["local/".Length..] : model;
}
