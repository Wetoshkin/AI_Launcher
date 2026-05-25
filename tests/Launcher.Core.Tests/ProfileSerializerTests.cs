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
}
