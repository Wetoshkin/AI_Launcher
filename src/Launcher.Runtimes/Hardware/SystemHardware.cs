namespace Launcher.Runtimes.Hardware;

/// <summary>
/// Снимок железа: процессор, видеокарты (с памятью) и системная RAM.
/// Используется для диаграммы загрузки модели в память.
/// </summary>
public sealed record SystemHardware(
    string CpuName,
    IReadOnlyList<GpuInfo> Gpus,
    double RamTotalGb,
    double RamFreeGb)
{
    public static SystemHardware Empty { get; } =
        new("неизвестно", Array.Empty<GpuInfo>(), 0.0, 0.0);

    /// <summary>Есть ли дискретная/встроенная видеокарта с выделенной памятью.</summary>
    public bool HasGpu => Gpus.Count > 0;
}
