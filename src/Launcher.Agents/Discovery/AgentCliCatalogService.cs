using System.Linq;
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Discovery;

public sealed class AgentCliCatalogService(IExecutableResolver resolver)
{
    private static readonly AgentKind[] SupportedAgents = AgentCatalog.Supported.ToArray();

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

    public static string ExecutableName(AgentKind agent) => AgentCatalog.Get(agent).Executable;
}
