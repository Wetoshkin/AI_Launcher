namespace Launcher.Core.Parameters;

public sealed record ParameterHelp(
    string Id,
    string DisplayName,
    string ShortText,
    string Details,
    ParameterRiskLevel Risk);
