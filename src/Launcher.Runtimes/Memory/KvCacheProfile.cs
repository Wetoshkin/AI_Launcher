namespace Launcher.Runtimes.Memory;

public sealed record KvCacheProfile(string CacheK, string CacheV)
{
    public static KvCacheProfile Symmetric(string cacheType) => new(cacheType, cacheType);

    public bool IsTurboQuant =>
        IsTurboQuantCache(CacheK) || IsTurboQuantCache(CacheV);

    private static bool IsTurboQuantCache(string cacheType) =>
        cacheType.StartsWith("turbo", StringComparison.OrdinalIgnoreCase)
        || cacheType.StartsWith("tbq", StringComparison.OrdinalIgnoreCase);
}
