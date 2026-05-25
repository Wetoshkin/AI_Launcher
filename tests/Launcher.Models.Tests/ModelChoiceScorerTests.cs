using Launcher.Models.HuggingFace;

namespace Launcher.Models.Tests;

public sealed class ModelChoiceScorerTests
{
    [Fact]
    public void RewardsPopularityAndCompatibility()
    {
        var model = new HuggingFaceModelSummary(
            Id: "unsloth/Qwen3-Coder-GGUF",
            Downloads: 3_000_000,
            Likes: 600,
            Tags: new[] { "gguf", "qwen", "text-generation", "imatrix" },
            IsCompatibleWithCurrentGpu: true,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true);

        var score = ModelChoiceScorer.Score(model);

        Assert.True(score.Value > 90);
        Assert.Contains("HF popularity", score.Reasons);
        Assert.Contains("fits current GPU", score.Reasons);
    }
}
