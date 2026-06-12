namespace Launcher.Runtimes.Hardware;

public interface IHardwareProbe
{
    Task<SystemHardware> GetHardwareAsync(CancellationToken cancellationToken);
}
