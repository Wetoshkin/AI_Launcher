using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public sealed class KiloCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        return new LaunchPlan(
            "kilo",
            new[] { "-m", request.ProviderModel, request.ProjectPath },
            new Dictionary<string, string>
            {
                ["OPENAI_BASE_URL"] = request.BaseUrl,
                ["OPENAI_API_KEY"] = "local"
            });
    }
}
