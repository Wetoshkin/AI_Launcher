namespace Launcher.Runtimes.LlamaCpp;

public enum RuntimeReleaseAssetSource
{
    Stable,
    Latest,
    Manual,
    Detected
}

public static class RuntimeReleaseAssetSources
{
    public static RuntimeReleaseAssetSource Normalize(string? value)
    {
        var key = NormalizeKey(value);
        return key switch
        {
            "" => RuntimeReleaseAssetSource.Manual,
            "stable" or "stablerelease" => RuntimeReleaseAssetSource.Stable,
            "latest" or "latestrelease" => RuntimeReleaseAssetSource.Latest,
            "manual" or "manualselection" or "manuallyselected" => RuntimeReleaseAssetSource.Manual,
            "detected" or "detectedruntime" or "autodetected" => RuntimeReleaseAssetSource.Detected,
            _ => throw new ArgumentException($"Unknown runtime release asset source: {value}", nameof(value))
        };
    }

    public static string ToLabel(RuntimeReleaseAssetSource source) => source switch
    {
        RuntimeReleaseAssetSource.Stable => "стабильный релиз",
        RuntimeReleaseAssetSource.Latest => "последний релиз",
        RuntimeReleaseAssetSource.Manual => "выбран вручную",
        RuntimeReleaseAssetSource.Detected => "обнаружен автоматически",
        _ => source.ToString()
    };

    private static string NormalizeKey(string? value) =>
        new((value ?? string.Empty)
            .Trim()
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
}
