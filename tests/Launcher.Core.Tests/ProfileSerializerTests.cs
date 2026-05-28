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
            ]);

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
    }
}
