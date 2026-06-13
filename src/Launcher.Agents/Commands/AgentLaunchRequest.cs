using Launcher.Core.Scenarios;

namespace Launcher.Agents.Commands;

public sealed record AgentLaunchRequest(
    AgentKind Agent,
    string ProjectPath,
    string ProviderModel,
    string BaseUrl,
    int ContextTokens = 32768);
