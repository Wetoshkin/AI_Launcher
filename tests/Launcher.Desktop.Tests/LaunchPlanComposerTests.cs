using Launcher.Core.Decoding;
using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;
using Launcher.Desktop.ViewModels;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.Tests;

public sealed class LaunchPlanComposerTests
{
    [Fact]
    public void BuildServerPlanUsesDetectedRuntimeExecutableWhenAvailable()
    {
        var profile = Profile(LaunchMode.Endpoint, AgentKind.None);
        var runtime = new LlamaRuntimeInfo(
            @"D:\AI\runtimes\b5400\llama-server.exe",
            new LlamaServerCapabilities(
                new HashSet<string>(),
                new HashSet<string>(),
                new HashSet<string>(),
                SupportsTurboQuant: true,
                SupportsMtp: true));

        var plan = LaunchPlanComposer.BuildServerPlan(
            profile,
            DecodingPresetCatalog.Get("coding-safe"),
            runtime);

        Assert.Equal(@"D:\AI\runtimes\b5400\llama-server.exe", plan.Executable);
    }

    [Fact]
    public void BuildAgentScenarioPreviewShowsSeparateServerAndAgentStages()
    {
        var profile = Profile(LaunchMode.Agent, AgentKind.Kilo);

        var preview = LaunchPlanComposer.BuildAgentScenarioPreview(
            profile,
            DecodingPresetCatalog.Get("coding-safe"),
            runtime: null);

        Assert.Contains("SERVER:", preview.CommandLine);
        Assert.Contains("AGENT:", preview.CommandLine);
        Assert.Contains("local/qwen", preview.CommandLine);
        Assert.Contains(preview.EnvironmentLines, line => line.StartsWith("AGENT:", StringComparison.Ordinal));
    }

    private static LaunchProfile Profile(LaunchMode mode, AgentKind agent) =>
        new(
            Id: "test",
            Name: "test",
            Mode: mode,
            Agent: agent,
            Runtime: RuntimeKind.LlamaCppTurboQuant,
            ProjectPath: @"D:\AI\Projects\demo",
            ModelPath: @"D:\AI\Models\qwen.gguf",
            ContextTokens: 4096,
            Port: 18080,
            AntiLoopPresetId: "coding-safe");
}
