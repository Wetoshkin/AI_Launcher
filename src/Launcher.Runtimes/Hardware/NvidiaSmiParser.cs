using System.Globalization;

namespace Launcher.Runtimes.Hardware;

public static class NvidiaSmiParser
{
    public static IReadOnlyList<GpuInfo> ParseGpuRows(string output)
    {
        var gpus = new List<GpuInfo>();

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            if (TryParseMemoryGb(parts[1], out var usedGb)
                && TryParseMemoryGb(parts[2], out var totalGb))
            {
                gpus.Add(new GpuInfo(parts[0], usedGb, totalGb));
            }
        }

        return gpus;
    }

    private static bool TryParseMemoryGb(string value, out double gb)
    {
        var text = value
            .Replace("MiB", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("MB", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var mib))
        {
            gb = mib / 1024.0;
            return true;
        }

        gb = 0.0;
        return false;
    }
}
