namespace Launcher.Runtimes.Memory;

public static class ContextRecommender
{
    private static readonly int[] BaseCandidates = [8192, 16384, 32768, 65536, 131072];

    public static int Recommend(ModelMemorySpec model, MemoryBudget budget, KvCacheProfile kvCache)
    {
        var maxContext = ContextCeiling(model, kvCache);
        var viable = ContextCandidates(model, kvCache)
            .Where(context => context <= maxContext)
            .Where(context => MemoryEstimator.Estimate(model, context, kvCache).TotalGb <= budget.FreeGb * 0.88)
            .ToArray();

        return viable.Length > 0 ? viable.Max() : 8192;
    }

    public static IReadOnlyList<int> ContextCandidates(ModelMemorySpec model, KvCacheProfile kvCache)
    {
        var candidates = BaseCandidates.ToList();
        if (kvCache.IsTurboQuant)
        {
            candidates.Add(262144);
        }

        if (model.NativeContextTokens is > 0 and var nativeContext)
        {
            candidates.Add(nativeContext);
        }

        return candidates.Distinct().Order().ToArray();
    }

    private static int ContextCeiling(ModelMemorySpec model, KvCacheProfile kvCache)
    {
        var ceiling = kvCache.IsTurboQuant
            ? model.NativeContextTokens ?? 131072
            : 131072;

        return Math.Max(8192, model.NativeContextTokens is > 0
            ? Math.Min(ceiling, model.NativeContextTokens.Value)
            : ceiling);
    }
}
