using Launcher.Core.Scenarios;

namespace Launcher.Agents.Discovery;

public sealed class AgentCliCatalogService(IExecutableResolver resolver)
{
    private static readonly AgentKind[] SupportedAgents =
    [
        AgentKind.OpenCode,
        AgentKind.Kilo,
        AgentKind.Claw,
        AgentKind.Aider,
        AgentKind.Pi
    ];

    public async Task<IReadOnlyList<AgentCliStatus>> CheckAsync(CancellationToken cancellationToken)
    {
        var statuses = new List<AgentCliStatus>();
        foreach (var agent in SupportedAgents)
        {
            statuses.Add(await CheckAsync(agent, cancellationToken));
        }

        return statuses;
    }

    public async Task<AgentCliStatus> CheckAsync(AgentKind agent, CancellationToken cancellationToken)
    {
        var executable = ExecutableName(agent);
        var path = await resolver.FindExecutableAsync(executable, cancellationToken);
        var version = path is null
            ? null
            : await resolver.GetVersionAsync(executable, cancellationToken);

        return new AgentCliStatus(agent, executable, path is not null, path, version);
    }

    public static string ExecutableName(AgentKind agent) => agent switch
    {
        AgentKind.OpenCode => "opencode",
        AgentKind.Kilo => "kilo",
        AgentKind.Claw => "claw",
        AgentKind.Aider => "aider",
        AgentKind.Pi => "pi",
        _ => throw new ArgumentOutOfRangeException(nameof(agent), agent, "Agent does not have a CLI executable.")
    };
}
