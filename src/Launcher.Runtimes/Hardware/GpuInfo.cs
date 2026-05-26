namespace Launcher.Runtimes.Hardware;

public sealed record GpuInfo(string Name, double UsedGb, double TotalGb)
{
    public double FreeGb => Math.Max(0.0, TotalGb - UsedGb);
}
