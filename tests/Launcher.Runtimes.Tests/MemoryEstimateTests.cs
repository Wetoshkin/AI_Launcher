using Launcher.Runtimes.Memory;

namespace Launcher.Runtimes.Tests;

public sealed class MemoryEstimateTests
{
    [Fact]
    public void EstimateIncludesWeightsKvCacheAndOverhead()
    {
        var estimate = MemoryEstimator.Estimate(
            new ModelMemorySpec(SizeGb: 15.5, ParametersBillion: 26.0, NativeContextTokens: 262144),
            contextTokens: 8192,
            KvCacheProfile.Symmetric("q8_0"));

        Assert.Equal(15.5, estimate.WeightsGb);
        Assert.Equal(0.669, estimate.KvCacheGb, 3);
        Assert.Equal(1.55, estimate.OverheadGb, 3);
        Assert.Equal(17.719, estimate.TotalGb, 3);
    }

    [Fact]
    public void TurboQuantKvCacheReducesEstimatedMemoryAndRaisesRecommendedContext()
    {
        var model = new ModelMemorySpec(SizeGb: 20.0, ParametersBillion: 26.0, NativeContextTokens: 262144);
        var budget = new MemoryBudget(FreeGb: 45.0, TotalGb: 45.0);

        var q8 = ContextRecommender.Recommend(model, budget, KvCacheProfile.Symmetric("q8_0"));
        var turbo = ContextRecommender.Recommend(model, budget, new KvCacheProfile("q8_0", "turbo4"));

        Assert.True(turbo > q8);
        Assert.Equal(262144, turbo);
    }
}
