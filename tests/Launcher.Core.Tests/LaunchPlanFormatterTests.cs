using Launcher.Core.LaunchPlans;

namespace Launcher.Core.Tests;

public sealed class LaunchPlanFormatterTests
{
    [Fact]
    public void FormatsExecutableArgumentsAndEnvironmentForPreview()
    {
        var plan = new LaunchPlan(
            "llama-server",
            new[] { "-m", @"D:\AI\Models\qwen coder.gguf", "--port", "8080" },
            new Dictionary<string, string> { ["OPENAI_API_KEY"] = "local" });

        var preview = LaunchPlanFormatter.Format(plan);

        Assert.Equal("""llama-server -m "D:\AI\Models\qwen coder.gguf" --port 8080""", preview.CommandLine);
        Assert.Contains("OPENAI_API_KEY=local", preview.EnvironmentLines);
    }
}
