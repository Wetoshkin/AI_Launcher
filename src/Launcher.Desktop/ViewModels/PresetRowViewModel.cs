using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Desktop.ViewModels;

public sealed class PresetRowViewModel(LaunchProfile profile)
{
    public LaunchProfile Profile => profile;

    public string Name => profile.Name;

    public string Summary => $"{ModeLabel(profile.Mode)} · {AgentLabel(profile.Agent)} · {profile.Runtime} · {profile.ContextTokens / 1024}k · порт {profile.Port}";

    public string ProjectPath => profile.ProjectPath ?? "без проекта";

    public string ModelPath => profile.ModelPath;

    private static string ModeLabel(LaunchMode mode) => mode switch
    {
        LaunchMode.Agent => "Проект",
        LaunchMode.Endpoint => "Сервер",
        _ => mode.ToString()
    };

    private static string AgentLabel(AgentKind agent) => agent switch
    {
        AgentKind.None => "без агента",
        _ => agent.ToString()
    };
}
