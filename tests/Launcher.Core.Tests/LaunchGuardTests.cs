using Launcher.Core.Guards;
using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Core.Tests;

public sealed class LaunchGuardTests
{
    [Fact]
    public void BlocksAgentWithoutProjectPath()
    {
        var result = LaunchGuard.Validate(Profile(LaunchMode.Agent, AgentKind.Kilo, null, @"D:\AI\Models\qwen.gguf"));

        Assert.False(result.CanLaunch);
        Assert.Contains("папка проекта", result.Messages[0]);
    }

    [Fact]
    public void BlocksMissingModelPath()
    {
        var result = LaunchGuard.Validate(Profile(LaunchMode.Endpoint, AgentKind.None, null, "модель не выбрана"));

        Assert.False(result.CanLaunch);
        Assert.Contains("модель", result.Messages[0]);
    }

    [Fact]
    public void AllowsEndpointWithModel()
    {
        var result = LaunchGuard.Validate(Profile(LaunchMode.Endpoint, AgentKind.None, null, @"D:\AI\Models\qwen.gguf"));

        Assert.True(result.CanLaunch);
        Assert.Empty(result.Messages);
    }

    private static LaunchProfile Profile(LaunchMode mode, AgentKind agent, string? projectPath, string modelPath) => new(
        Id: "draft",
        Name: "Draft",
        Mode: mode,
        Agent: agent,
        Runtime: RuntimeKind.LlamaCppMtp,
        ProjectPath: projectPath,
        ModelPath: modelPath,
        ContextTokens: 65536,
        Port: 8080,
        AntiLoopPresetId: "coding-safe");
}
