using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

/// <summary>
/// Goose (Block) запускается как «goose session» с провайдером, заданным через переменные окружения:
/// OpenAI-совместимый endpoint локального сервера.
/// </summary>
public sealed class GooseCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        var model = StripLocalPrefix(request.ProviderModel);

        return new LaunchPlan(
            "goose",
            new[] { "session" },
            new Dictionary<string, string>
            {
                ["GOOSE_PROVIDER"] = "openai",
                ["GOOSE_MODEL"] = model,
                ["OPENAI_HOST"] = request.BaseUrl,
                ["OPENAI_BASE_PATH"] = "v1/chat/completions",
                ["OPENAI_API_KEY"] = "local"
            });
    }

    private static string StripLocalPrefix(string model) =>
        model.StartsWith("local/", System.StringComparison.Ordinal) ? model["local/".Length..] : model;
}
