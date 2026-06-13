using System.Collections.Generic;
using System.Linq;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.Services;

/// <summary>
/// Различает дискретные видеокарты и встроенную графику (iGPU).
/// Встройка по умолчанию не используется под LLM — только под отрисовку Windows.
/// </summary>
public static class GpuClassifier
{
    public static bool IsDiscrete(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var n = name.ToLowerInvariant();
        if (n.Contains("nvidia") || n.Contains("geforce") || n.Contains("rtx")
            || n.Contains("gtx") || n.Contains("quadro") || n.Contains("tesla"))
        {
            return true;
        }

        // Дискретные AMD; "radeon graphics" без RX/Pro — это встройка.
        if (n.Contains("radeon rx") || n.Contains("radeon pro") || n.Contains("instinct"))
        {
            return true;
        }

        // Дискретные Intel Arc (Arc A-серии), но не встроенная графика Intel.
        if (n.Contains("intel arc") || n.Contains(" arc a"))
        {
            return true;
        }

        return false;
    }

    public static bool IsDiscrete(GpuInfo gpu) => IsDiscrete(gpu.Name);

    public static bool IsIntegrated(GpuInfo gpu) => !IsDiscrete(gpu.Name);

    /// <summary>Карты, которые реально задействуем под LLM (дискретные + встройка, если она включена).</summary>
    public static IReadOnlyList<GpuInfo> UsableGpus(SystemHardware hardware, bool useIntegrated) =>
        hardware.Gpus.Where(g => IsDiscrete(g) || useIntegrated).ToList();

    /// <summary>Суммарная видеопамять, которую учитываем в расчётах «влезет ли модель».</summary>
    public static double UsableVramGb(SystemHardware hardware, bool useIntegrated) =>
        UsableGpus(hardware, useIntegrated).Sum(g => g.TotalGb);
}
