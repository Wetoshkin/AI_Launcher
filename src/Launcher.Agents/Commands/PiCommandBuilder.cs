using Launcher.Core.LaunchPlans;

namespace Launcher.Agents.Commands;

/// <summary>
/// Pi запускается в папке проекта и сам подхватывает расширение из .pi/extensions/
/// (его пишет AgentProjectConfigWriter), где зарегистрирован локальный OpenAI-провайдер.
/// </summary>
public sealed class PiCommandBuilder : IAgentCommandBuilder
{
    public LaunchPlan Build(AgentLaunchRequest request)
    {
        LocalOpenAiCommandRequestValidator.Validate(request);

        return new LaunchPlan(
            "pi",
            System.Array.Empty<string>(),
            new Dictionary<string, string>());
    }
}
