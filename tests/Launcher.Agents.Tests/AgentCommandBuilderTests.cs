using Launcher.Agents.Commands;
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Tests;

public sealed class AgentCommandBuilderTests
{
    [Fact]
    public void KiloCommandUsesProjectAndProviderModel()
    {
        var plan = new KiloCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.Kilo,
            @"D:\AI\Projects\App",
            "local/llama.cpp/qwen",
            "http://127.0.0.1:8080/v1"));

        Assert.Equal("kilo", plan.Executable);
        Assert.Contains("-m", plan.Arguments);
        Assert.Contains(@"D:\AI\Projects\App", plan.Arguments);
    }

    [Fact]
    public void OpenCodeCommandUsesLocalOllamaProviderEnvironment()
    {
        var plan = new OpenCodeCommandBuilder().Build(new AgentLaunchRequest(
            AgentKind.OpenCode,
            @"D:\AI\Projects\App",
            "qwen",
            "http://127.0.0.1:11434/v1"));

        Assert.Equal("opencode", plan.Executable);
        Assert.Contains(@"D:\AI\Projects\App", plan.Arguments);
        Assert.Equal("http://127.0.0.1:11434/v1", plan.Environment["OPENAI_BASE_URL"]);
        Assert.Equal("local", plan.Environment["OPENAI_API_KEY"]);
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
}
