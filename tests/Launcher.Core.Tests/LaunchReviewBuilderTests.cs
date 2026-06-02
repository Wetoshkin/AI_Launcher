using Launcher.Core.Profiles;
using Launcher.Core.Review;
using Launcher.Core.Scenarios;

namespace Launcher.Core.Tests;

public sealed class LaunchReviewBuilderTests
{
    [Fact]
    public void BuildsRussianReviewForAgentProfile()
    {
        var profile = new LaunchProfile(
            Id: "p1",
            Name: "Kilo Qwen",
            Mode: LaunchMode.Agent,
            Agent: AgentKind.Kilo,
            Runtime: RuntimeKind.LlamaCppTurboQuant,
            ProjectPath: @"D:\AI\Projects\App",
            ModelPath: @"D:\AI\Models\qwen.gguf",
            ContextTokens: 65536,
            Port: 8080,
            AntiLoopPresetId: "coding-safe");

        var review = LaunchReviewBuilder.Build(profile);

        Assert.Contains("Режим: проект", review.Lines);
        Assert.Contains("Агент: Kilo", review.Lines);
        Assert.Contains("Runtime: llama.cpp TurboQuant", review.Lines);
        Assert.Contains("Контекст: 65 536 токенов", review.Lines);
        Assert.Contains("Порт: 8080", review.Lines);
    }

    [Fact]
    public void IncludesSpeculativeDraftMinTokensWhenMtpIsEnabled()
    {
        var profile = new LaunchProfile(
            Id: "p1",
            Name: "MTP Endpoint",
            Mode: LaunchMode.Endpoint,
            Agent: AgentKind.None,
            Runtime: RuntimeKind.LlamaCppMtp,
            ProjectPath: null,
            ModelPath: @"D:\AI\Models\qwen.gguf",
            ContextTokens: 65536,
            Port: 8080,
            AntiLoopPresetId: "mtp-fast")
        {
            Mtp = new MtpSettings(
                Enabled: true,
                DraftModelPath: null,
                DraftTokens: 4,
                DraftMinTokens: 2,
                SpeculativeType: "draft-mtp")
        };

        var review = LaunchReviewBuilder.Build(profile);

        Assert.Contains("Speculative decoding: draft-mtp, draft min/max: 2/4", review.Lines);
    }
}
