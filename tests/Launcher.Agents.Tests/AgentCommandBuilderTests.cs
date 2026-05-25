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
}
