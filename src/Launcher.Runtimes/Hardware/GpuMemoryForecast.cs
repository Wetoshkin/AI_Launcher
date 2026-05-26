namespace Launcher.Runtimes.Hardware;

public sealed record GpuMemoryForecast(
    IReadOnlyList<GpuMemoryForecastRow> Rows,
    double OverflowGb);

public sealed record GpuMemoryForecastRow(
    GpuInfo Gpu,
    double AddedGb,
    double ProjectedUsedGb,
    double FreeAfterGb);
