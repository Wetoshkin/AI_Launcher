using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public sealed class AiderCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        return new LaunchPlan(
            "aider",
            new[] { "--model", $"openai/{request.ProviderModel}", request.ProjectPath },
            new Dictionary<string, string>
            {
                ["OPENAI_API_BASE"] = request.BaseUrl,
                ["OPENAI_API_KEY"] = "local"
            });
    }
}
