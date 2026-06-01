namespace Launcher.Agents.Commands;

public sealed record AgentProjectConfigResult(
    bool Written,
    string? ConfigPath,
    string Message);
