using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public sealed class PiCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        return new LaunchPlan(
            "pi",
            new[] { "--model", request.ProviderModel, request.ProjectPath },
            new Dictionary<string, string>
            {
                ["OPENAI_BASE_URL"] = request.BaseUrl,
                ["OPENAI_API_KEY"] = "local"
            });
    }
}
