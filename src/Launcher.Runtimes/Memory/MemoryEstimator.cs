namespace Launcher.Runtimes.Memory;

public static class MemoryEstimator
{
    private const double Q8BitsPerValue = 8.5;

    private static readonly IReadOnlyDictionary<string, double> CacheBitsPerValue =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["f16"] = 16.0,
            ["fp16"] = 16.0,
            ["q8_0"] = 8.5,
            ["q8"] = 8.5,
            ["q6_k"] = 6.5,
            ["q5_0"] = 5.5,
            ["q5_1"] = 5.5,
            ["q4_0"] = 4.5,
            ["q4_1"] = 4.5,
            ["turbo4"] = 4.25,
            ["tbq4_0"] = 4.0625,
            ["turbo3"] = 3.125,
            ["tbq3_0"] = 3.0625,
            ["turbo2"] = 2.125
        };

    public static MemoryEstimate Estimate(
        ModelMemorySpec model,
        int contextTokens,
        KvCacheProfile kvCache)
    {
        var kvBitsPerValue = KvBitsPerValue(kvCache);
        var kvFactor = 0.18 * (kvBitsPerValue / Q8BitsPerValue);
        var safeParameters = Math.Max(model.ParametersBillion, 1.0);
        var kvGb = (contextTokens / 8192.0) * (safeParameters / 7.0) * kvFactor;
        var overheadGb = Math.Max(1.0, model.SizeGb * 0.10);
        var totalGb = model.SizeGb + kvGb + overheadGb;

        return new MemoryEstimate(model.SizeGb, kvGb, kvBitsPerValue, overheadGb, totalGb);
    }

    public static double KvBitsPerValue(KvCacheProfile kvCache)
    {
        var kBits = CacheBitsPerValue.GetValueOrDefault(kvCache.CacheK, 16.0);
        var vBits = CacheBitsPerValue.GetValueOrDefault(kvCache.CacheV, kBits);
        return (kBits + vBits) / 2.0;
    }
}
