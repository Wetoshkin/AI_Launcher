namespace Launcher.Models.HuggingFace;

using System.Globalization;

internal static class HuggingFaceFileSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long? sizeBytes)
    {
        if (sizeBytes is null or <= 0)
        {
            return "";
        }

        var size = (double)sizeBytes.Value;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return size >= 10 || Math.Abs(size - Math.Round(size)) < 0.05
            ? string.Create(CultureInfo.InvariantCulture, $"{Math.Round(size):0} {Units[unitIndex]}")
            : string.Create(CultureInfo.InvariantCulture, $"{size:0.#} {Units[unitIndex]}");
    }
}
