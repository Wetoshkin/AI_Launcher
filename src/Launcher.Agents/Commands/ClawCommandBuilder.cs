using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public sealed class ClawCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        return new LaunchPlan(
            "claw",
            new[] { "--model", request.ProviderModel, request.ProjectPath },
            new Dictionary<string, string>
            {
                ["OPENAI_BASE_URL"] = request.BaseUrl,
                ["OPENAI_API_KEY"] = "local"
            });
    }
}
