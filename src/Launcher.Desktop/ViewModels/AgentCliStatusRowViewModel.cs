using Launcher.Agents.Discovery;

namespace Launcher.Desktop.ViewModels;

public sealed class AgentCliStatusRowViewModel(AgentCliStatus status)
{
    public string Name => status.Agent.ToString();

    public string Executable => status.ExecutableName;

    public string Status => status.StatusText;

    public string Path => status.ExecutablePath ?? "не найден в PATH";

    public bool IsInstalled => status.IsInstalled;
}
