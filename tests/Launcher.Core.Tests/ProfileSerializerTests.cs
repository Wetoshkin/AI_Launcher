using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Core.Tests;

public sealed class ProfileSerializerTests
{
    [Fact]
    public void RoundTripsProfileWithRussianNameAndRuntimeFields()
    {
        var profile = new LaunchProfile(
            Id: "kilo-qwen",
            Name: "Kilo через Qwen",
            Mode: LaunchMode.Agent,
            Agent: AgentKind.Kilo,
            Runtime: RuntimeKind.LlamaCppTurboQuant,
            ProjectPath: @"D:\AI\Projects\Test",
            ModelPath: @"D:\AI\Models\qwen.gguf",
            ContextTokens: 65536,
            Port: 8080,
            AntiLoopPresetId: "code-stable");

        var json = ProfileSerializer.Serialize(profile);
        var restored = ProfileSerializer.DeserializeProfile(json);

        Assert.Equal(profile, restored);
    }

    [Fact]
    public void RoundTripsProfileWithKvAndMtpParameters()
    {
        var profile = new LaunchProfile(
            Id: "opencode-qwen-tq",
            Name: "OpenCode TurboQuant",
            Mode: LaunchMode.Agent,
            Agent: AgentKind.OpenCode,
            Runtime: RuntimeKind.LlamaCppTurboQuant,
            ProjectPath: @"D:\AI\Projects\Test",
            ModelPath: @"D:\AI\Models\qwen.gguf",
            ContextTokens: 131072,
            Port: 9091,
            AntiLoopPresetId: "coding-safe")
        {
            KvCache = new KvCacheSettings(
                TypeK: "q8_0",
                TypeV: "q6_k",
                FlashAttention: true,
                OffloadKqv: false),
            Mtp = new MtpSettings(
                Enabled: true,
                DraftModelPath: @"D:\AI\Models\qwen-draft.gguf",
                DraftTokens: 4,
                SpeculativeType: "mtp")
        };

        var json = ProfileSerializer.Serialize(profile);
        var restored = ProfileSerializer.DeserializeProfile(json);

        Assert.Equal(profile, restored);
        Assert.Equal("q8_0", restored.KvCache?.TypeK);
        Assert.Equal("q6_k", restored.KvCache?.TypeV);
        Assert.True(restored.KvCache?.FlashAttention);
        Assert.False(restored.KvCache?.OffloadKqv);
        Assert.True(restored.Mtp?.Enabled);
        Assert.Equal(profile.Mtp, restored.Mtp);
    }

    [Fact]
    public void RoundTripsLauncherSettingsWithProfiles()
    {
        var settings = new LauncherSettings(
            ModelsRoot: @"D:\AI\Models",
            ProjectsRoot: @"D:\AI\Projects",
            RuntimeRoot: @"D:\AI\runtimes",
            DownloadsRoot: @"D:\AI\downloads",
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "pro",
            Profiles:
            [
                new LaunchProfile(
                    Id: "kilo-qwen",
                    Name: "Kilo через Qwen",
                    Mode: LaunchMode.Agent,
                    Agent: AgentKind.Kilo,
                    Runtime: RuntimeKind.LlamaCppTurboQuant,
                    ProjectPath: @"D:\AI\Projects\Test",
                    ModelPath: @"D:\AI\Models\qwen.gguf",
                    ContextTokens: 65536,
                    Port: 8080,
                    AntiLoopPresetId: "coding-safe")
            ])
        {
            LastRuntimeVersionSource = @"D:\AI\runtimes\b5400\llama-server.exe"
        };

        var json = ProfileSerializer.SerializeSettings(settings);
        var restored = ProfileSerializer.DeserializeSettings(json);

        Assert.Equal(settings.ModelsRoot, restored.ModelsRoot);
        Assert.Equal(settings.ProjectsRoot, restored.ProjectsRoot);
        Assert.Equal(settings.RuntimeRoot, restored.RuntimeRoot);
        Assert.Equal(settings.DownloadsRoot, restored.DownloadsRoot);
        Assert.Equal(settings.DefaultPort, restored.DefaultPort);
        Assert.Equal(settings.Language, restored.Language);
        Assert.Equal(settings.HelpMode, restored.HelpMode);
        Assert.Equal(settings.Profiles, restored.Profiles);
        Assert.Equal(settings.LastRuntimeVersionSource, restored.LastRuntimeVersionSource);
    }

    [Fact]
    public void RoundTripsLauncherSettingsWithHuggingFaceFilters()
    {
        var settings = new LauncherSettings(
            ModelsRoot: @"D:\AI\Models",
            ProjectsRoot: @"D:\AI\Projects",
            RuntimeRoot: @"D:\AI\runtimes",
            DownloadsRoot: @"D:\AI\downloads",
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "pro",
            Profiles: [])
        {
            HuggingFaceFilters = new HuggingFaceFilterSettings(
                SearchQuery: "qwen coder",
                Author: "Qwen",
                Quantization: "Q4_K_M",
                Architecture: "qwen2",
                Task: "text-generation",
                Sort: "downloads",
                ShowGated: false,
                ShowIncompatible: true)
        };

        var json = ProfileSerializer.SerializeSettings(settings);
        var restored = ProfileSerializer.DeserializeSettings(json);

        Assert.Equal(settings.HuggingFaceFilters, restored.HuggingFaceFilters);
    }
}
