using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

public interface IAgentCommandBuilder
{
    LaunchPlan Build(AgentLaunchRequest request);
}
