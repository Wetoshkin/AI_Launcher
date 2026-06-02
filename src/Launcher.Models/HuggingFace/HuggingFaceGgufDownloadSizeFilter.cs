namespace Launcher.Models.HuggingFace;

public static class HuggingFaceGgufDownloadSizeFilter
{
    private const long Gibibyte = 1024L * 1024 * 1024;

    public static IReadOnlyList<HuggingFaceGgufDownloadOption> Apply(
        IEnumerable<HuggingFaceGgufDownloadOption> options,
        HuggingFaceGgufDownloadSizeRange range)
    {
        if (range == HuggingFaceGgufDownloadSizeRange.Any)
            return options.ToList();

        return options.Where(option => Matches(option.TotalSizeBytes, range)).ToList();
    }

    private static bool Matches(long? sizeBytes, HuggingFaceGgufDownloadSizeRange range)
    {
        if (range == HuggingFaceGgufDownloadSizeRange.Unknown)
            return !sizeBytes.HasValue;

        if (!sizeBytes.HasValue)
            return false;

        return range switch
        {
            HuggingFaceGgufDownloadSizeRange.UpTo4Gb => sizeBytes.Value <= 4 * Gibibyte,
            HuggingFaceGgufDownloadSizeRange.UpTo8Gb => sizeBytes.Value <= 8 * Gibibyte,
            HuggingFaceGgufDownloadSizeRange.Between8And16Gb => sizeBytes.Value > 8 * Gibibyte && sizeBytes.Value <= 16 * Gibibyte,
            HuggingFaceGgufDownloadSizeRange.UpTo16Gb => sizeBytes.Value <= 16 * Gibibyte,
            HuggingFaceGgufDownloadSizeRange.Between16And32Gb => sizeBytes.Value > 16 * Gibibyte && sizeBytes.Value <= 32 * Gibibyte,
            HuggingFaceGgufDownloadSizeRange.Over16Gb => sizeBytes.Value > 16 * Gibibyte,
            HuggingFaceGgufDownloadSizeRange.Over32Gb => sizeBytes.Value > 32 * Gibibyte,
            _ => true
        };
    }
}
