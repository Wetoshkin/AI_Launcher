using Launcher.Core.Scenarios;

namespace Launcher.Agents.Discovery;

public sealed record AgentCliStatus(
    AgentKind Agent,
    string ExecutableName,
    bool IsInstalled,
    string? ExecutablePath,
    string? VersionText)
{
    public string StatusText => IsInstalled ? VersionText ?? "установлен" : "не найден";
}
