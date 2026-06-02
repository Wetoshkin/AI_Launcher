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

    public static TheoryData<IAgentCommandBuilder, AgentKind> LocalOpenAiBuilders()
    {
        return new TheoryData<IAgentCommandBuilder, AgentKind>
        {
            { new OpenCodeCommandBuilder(), AgentKind.OpenCode },
            { new KiloCommandBuilder(), AgentKind.Kilo },
            { new AiderCommandBuilder(), AgentKind.Aider }
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
