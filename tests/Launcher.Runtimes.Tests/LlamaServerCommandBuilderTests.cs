using Launcher.Core.Decoding;
using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class LlamaServerCommandBuilderTests
{
    [Fact]
    public void BuildsSafeLlamaServerCommandWithoutMtpFlags()
    {
        var profile = Profile("coding-safe");

        var plan = LlamaServerCommandBuilder.Build(profile, DecodingPresetCatalog.Get("coding-safe"));

        Assert.Equal("llama-server", plan.Executable);
        Assert.Contains("-m", plan.Arguments);
        Assert.Contains(@"D:\AI\Models\qwen.gguf", plan.Arguments);
        Assert.Contains("--ctx-size", plan.Arguments);
        Assert.Contains("65536", plan.Arguments);
        Assert.DoesNotContain("--spec-type", plan.Arguments);
    }

    [Fact]
    public void BuildsMtpLlamaServerCommandWithSpecFlags()
    {
        var profile = Profile("mtp-fast");

        var plan = LlamaServerCommandBuilder.Build(profile, DecodingPresetCatalog.Get("mtp-fast"));

        Assert.Contains("--spec-type", plan.Arguments);
        Assert.Contains("draft-mtp", plan.Arguments);
        Assert.Contains("--spec-draft-n-max", plan.Arguments);
    }

    [Fact]
    public void BuildsStableOpenAiModelAliasFromGgufFileName()
    {
        var profile = Profile("coding-safe");

        var plan = LlamaServerCommandBuilder.Build(profile, DecodingPresetCatalog.Get("coding-safe"));

        Assert.Contains("--alias", plan.Arguments);
        Assert.Contains("local/qwen", plan.Arguments);
    }

    private static LaunchProfile Profile(string presetId) => new(
        Id: "p1",
        Name: "Endpoint",
        Mode: LaunchMode.Endpoint,
        Agent: AgentKind.None,
        Runtime: RuntimeKind.LlamaCppMtp,
        ProjectPath: null,
        ModelPath: @"D:\AI\Models\qwen.gguf",
        ContextTokens: 65536,
        Port: 8080,
        AntiLoopPresetId: presetId);
}
