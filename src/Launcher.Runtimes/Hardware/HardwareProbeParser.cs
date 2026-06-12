using System.Globalization;

namespace Launcher.Runtimes.Hardware;

/// <summary>
/// Парсит нормализованный вывод probe железа в <see cref="SystemHardware"/>.
/// Формат строк (одна на сущность):
///   CPU|&lt;имя&gt;
///   GPU|&lt;имя&gt;|&lt;байты выделенной памяти&gt;
///   RAM|&lt;всего байт&gt;|&lt;свободно байт&gt;
/// Видеокарты с нулевой памятью (виртуальные дисплеи) пропускаются.
/// </summary>
public static class HardwareProbeParser
{
    private const double BytesPerGb = 1024.0 * 1024.0 * 1024.0;

    public static SystemHardware Parse(string output)
    {
        var cpu = "неизвестно";
        var gpus = new List<GpuInfo>();
        var ramTotalGb = 0.0;
        var ramFreeGb = 0.0;

        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            switch (parts[0].Trim().ToUpperInvariant())
            {
                case "CPU" when parts.Length >= 2 && parts[1].Trim().Length > 0:
                    cpu = parts[1].Trim();
                    break;

                case "GPU" when parts.Length >= 3 && TryBytesToGb(parts[2], out var vramGb) && vramGb > 0.05:
                    gpus.Add(new GpuInfo(parts[1].Trim(), 0.0, vramGb));
                    break;

                case "RAM" when parts.Length >= 3
                    && TryBytesToGb(parts[1], out var totalGb)
                    && TryBytesToGb(parts[2], out var freeGb):
                    ramTotalGb = totalGb;
                    ramFreeGb = freeGb;
                    break;
            }
        }

        return new SystemHardware(cpu, gpus, ramTotalGb, ramFreeGb);
    }

    private static bool TryBytesToGb(string value, out double gb)
    {
        if (long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytes)
            && bytes > 0)
        {
            gb = bytes / BytesPerGb;
            return true;
        }

        gb = 0.0;
        return false;
    }
}
