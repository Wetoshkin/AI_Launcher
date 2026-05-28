using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimeCompatibilityServiceTests
{
    [Fact]
    public void CheckReturnsCompatibleForMtpRuntimeWithMtpCapability()
    {
        var profile = Profile(RuntimeKind.LlamaCppMtp);
        var runtime = Runtime(supportsMtp: true, supportsTurboQuant: false);

        var result = RuntimeCompatibilityService.Check(profile, runtime);

        Assert.True(result.IsCompatible);
        Assert.Contains("MTP поддерживается", result.Messages);
    }

    [Fact]
    public void CheckRejectsMtpRuntimeWithoutMtpCapability()
    {
        var profile = Profile(RuntimeKind.LlamaCppMtp);
        var runtime = Runtime(supportsMtp: false, supportsTurboQuant: true);

        var result = RuntimeCompatibilityService.Check(profile, runtime);

        Assert.False(result.IsCompatible);
        Assert.Contains("Выбран MTP, но runtime не поддерживает --spec-type draft-mtp.", result.Messages);
    }

    [Fact]
    public void CheckRejectsTurboQuantRuntimeWithoutTurboQuantCapability()
    {
        var profile = Profile(RuntimeKind.LlamaCppTurboQuant);
        var runtime = Runtime(supportsMtp: true, supportsTurboQuant: false);

        var result = RuntimeCompatibilityService.Check(profile, runtime);

        Assert.False(result.IsCompatible);
        Assert.Contains("Выбран TurboQuant, но runtime не поддерживает TurboQuant-флаги.", result.Messages);
    }

    [Fact]
    public void CheckAsksToScanRuntimeWhenLlamaRuntimeIsMissing()
    {
        var profile = Profile(RuntimeKind.LlamaCppMtp);

        var result = RuntimeCompatibilityService.Check(profile, runtime: null);

        Assert.False(result.IsCompatible);
        Assert.Contains("Runtime llama-server не проверен.", result.Messages);
    }

    private static LaunchProfile Profile(RuntimeKind runtime) => new(
        "draft",
        "draft",
        LaunchMode.Endpoint,
        AgentKind.None,
        runtime,
        ProjectPath: null,
        ModelPath: @"D:\AI\Models\model.gguf",
        ContextTokens: 65536,
        Port: 8080,
        AntiLoopPresetId: "coding-safe");

    private static LlamaRuntimeInfo Runtime(bool supportsMtp, bool supportsTurboQuant) => new(
        @"D:\AI\runtimes\llama-server.exe",
        new LlamaServerCapabilities(
            new HashSet<string>(),
            new HashSet<string>(),
            new HashSet<string>(),
            supportsTurboQuant,
            supportsMtp));
}
