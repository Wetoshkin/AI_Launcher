using Launcher.Agents.Commands;
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Tests;

public sealed class AgentCommandBuilderTests
{
    [Fact]
    public void KiloCommandUsesLocalEndpointProjectAndProviderModel()
    {
        var plan = new KiloCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.Kilo,
            @"D:\AI\Projects\App",
            "local/Qwen3-Coder-Q4_K_M",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("kilo", plan.Executable);
        Assert.Contains("-m", plan.Arguments);
        Assert.Contains("local/Qwen3-Coder-Q4_K_M", plan.Arguments);
        Assert.Contains(@"D:\AI\Projects\App", plan.Arguments);
        Assert.Equal("http://127.0.0.1:8080/v1", plan.Environment["OPENAI_BASE_URL"]);
        Assert.Equal("local", plan.Environment["OPENAI_API_KEY"]);
        AssertNoToolFlags(plan);
    }

    [Fact]
    public void OpenCodeCommandUsesLocalEndpointProjectAndProviderModel()
    {
        var plan = new OpenCodeCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.OpenCode,
            @"D:\AI\Projects\App",
            "local/Qwen3-Coder-Q4_K_M",
            "http://127.0.0.1:11434/v1"));

        Assert.Equal("opencode", plan.Executable);
        Assert.Contains("--model", plan.Arguments);
        Assert.Contains("local/Qwen3-Coder-Q4_K_M", plan.Arguments);
        Assert.Contains(@"D:\AI\Projects\App", plan.Arguments);
        Assert.Equal("http://127.0.0.1:11434/v1", plan.Environment["OPENAI_BASE_URL"]);
        Assert.Equal("local", plan.Environment["OPENAI_API_KEY"]);
        AssertNoToolFlags(plan);
    }

    [Fact]
    public void AiderCommandUsesLocalEndpointProjectAndProviderModel()
    {
        var plan = new AiderCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.Aider,
            @"D:\AI\Projects\App",
            "local/Qwen3-Coder-Q4_K_M",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("aider", plan.Executable);
        Assert.Contains("--model", plan.Arguments);
        Assert.Contains("openai/local/Qwen3-Coder-Q4_K_M", plan.Arguments);
        Assert.Contains(@"D:\AI\Projects\App", plan.Arguments);
        Assert.Equal("http://127.0.0.1:8080/v1", plan.Environment["OPENAI_API_BASE"]);
        Assert.Equal("local", plan.Environment["OPENAI_API_KEY"]);
        AssertNoToolFlags(plan);
    }

    [Theory]
    [MemberData(nameof(LocalOpenAiBuilders))]
    public void LocalOpenAiCommandBuildersRejectNonLocalProviderModel(
        IAgentCommandBuilder builder,
        AgentKind agent)
    {
        var request = new AgentLaunchRequest(
            agent,
            @"D:\AI\Projects\App",
            "qwen",
            "http://127.0.0.1:8080/v1");

        Assert.Throws<ArgumentException>(() => builder.Build(request));
    }

    [Fact]
    public void ClawCommandUsesOpenAiCompatibleEndpoint()
    {
        var plan = new ClawCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.Claw,
            @"D:\AI\Projects\App",
            "qwen",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("claw", plan.Executable);
        Assert.Contains("--model", plan.Arguments);
        Assert.Contains("qwen", plan.Arguments);
        Assert.Contains(@"D:\AI\Projects\App", plan.Arguments);
    }

    [Fact]
    public void CrushCommandRunsInProjectViaConfig()
    {
        var plan = new CrushCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.Crush,
            @"D:\AI\Projects\App",
            "local/Qwen3-Coder-Q4_K_M",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("crush", plan.Executable);
        Assert.Equal("http://127.0.0.1:8080/v1", plan.Environment["OPENAI_API_BASE"]);
    }

    [Fact]
    public void GooseCommandStartsSessionWithProviderEnv()
    {
        var plan = new GooseCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.Goose,
            @"D:\AI\Projects\App",
            "local/Qwen3-Coder-Q4_K_M",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("goose", plan.Executable);
        Assert.Contains("session", plan.Arguments);
        Assert.Equal("openai", plan.Environment["GOOSE_PROVIDER"]);
        Assert.Equal("Qwen3-Coder-Q4_K_M", plan.Environment["GOOSE_MODEL"]);
    }

    [Fact]
    public void ClaudeCodeUsesAnthropicEnvWithBareBaseUrl()
    {
        var plan = new ClaudeCodeCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.ClaudeCode,
            @"D:\AI\Projects\App",
            "local/Qwen3-Coder-Q4_K_M",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("claude", plan.Executable);
        Assert.Contains("--dangerously-skip-permissions", plan.Arguments);
        Assert.Equal("http://127.0.0.1:8080", plan.Environment["ANTHROPIC_BASE_URL"]);
        Assert.Equal("local", plan.Environment["ANTHROPIC_AUTH_TOKEN"]);
        // Claude Code-у подсовываем настоящий ID Opus 4.8, чтобы CLI считала это флагманской моделью.
        Assert.Equal("claude-opus-4-8", plan.Environment["ANTHROPIC_DEFAULT_OPUS_MODEL"]);
        Assert.Equal("claude-opus-4-8", plan.Environment["ANTHROPIC_DEFAULT_SONNET_MODEL"]);
        Assert.Equal("claude-opus-4-8", plan.Environment["ANTHROPIC_DEFAULT_HAIKU_MODEL"]);
        Assert.Equal("claude-opus-4-8", plan.Environment["ANTHROPIC_MODEL"]);
    }

    public static TheoryData<IAgentCommandBuilder, AgentKind> LocalOpenAiBuilders()
    {
        return new TheoryData<IAgentCommandBuilder, AgentKind>
        {
            { new OpenCodeCommandBuilder(), AgentKind.OpenCode },
            { new KiloCommandBuilder(), AgentKind.Kilo },
            { new AiderCommandBuilder(), AgentKind.Aider },
            { new CrushCommandBuilder(), AgentKind.Crush },
            { new GooseCommandBuilder(), AgentKind.Goose },
            { new PiCommandBuilder(), AgentKind.Pi }
        };
    }

    private static void AssertNoToolFlags(Launcher.Core.LaunchPlans.LaunchPlan plan)
    {
        Assert.DoesNotContain(plan.Arguments, argument =>
            argument.Contains("tool", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(plan.Environment.Keys, key =>
            key.Contains("tool", StringComparison.OrdinalIgnoreCase));
    }
}
