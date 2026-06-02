using Launcher.Core.Scenarios;

namespace Launcher.Core.Profiles;

public sealed partial record LaunchProfile(
    string Id,
    string Name,
    LaunchMode Mode,
    AgentKind Agent,
    RuntimeKind Runtime,
    string? ProjectPath,
    string ModelPath,
    int ContextTokens,
    int Port,
    string AntiLoopPresetId);

public sealed partial record LaunchProfile
{
    public KvCacheSettings? KvCache { get; init; }

    public MtpSettings? Mtp { get; init; }
}
