using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

/// <summary>
/// Crush (Charm) запускается в папке проекта и читает конфиг crush.json (его пишет
/// AgentProjectConfigWriter). Модель/провайдер задаются через конфиг, поэтому аргументы пустые.
/// </summary>
public sealed class CrushCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        return new LaunchPlan(
            "crush",
            System.Array.Empty<string>(),
            new Dictionary<string, string>
            {
                ["OPENAI_API_KEY"] = "local",
                ["OPENAI_API_BASE"] = request.BaseUrl
            });
    }
}
