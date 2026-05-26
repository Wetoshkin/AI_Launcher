namespace Launcher.Runtimes.Hardware;

public interface IGpuProbe
{
    Task<IReadOnlyList<GpuInfo>> GetGpusAsync(CancellationToken cancellationToken);
}
