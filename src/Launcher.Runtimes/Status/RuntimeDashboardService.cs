using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Ports;
using System.Globalization;

namespace Launcher.Runtimes.Status;

public sealed class RuntimeDashboardService(IGpuProbe gpuProbe, IPortInspector portInspector)
{
    public async Task<RuntimeDashboardSnapshot> CheckAsync(int port, CancellationToken cancellationToken)
    {
        var gpus = await SafeGetGpusAsync(cancellationToken);
        var portOwner = await portInspector.InspectAsync(port, cancellationToken);
        var usedGpuGb = gpus.Sum(gpu => gpu.UsedGb);
        var totalGpuGb = gpus.Sum(gpu => gpu.TotalGb);
        var gpuText = totalGpuGb > 0
            ? string.Create(CultureInfo.InvariantCulture, $"GPU: {usedGpuGb:0.0} / {totalGpuGb:0.0} ГБ")
            : "GPU: нет данных";
        var portText = portOwner is null
            ? $"порт {port}: свободен"
            : $"порт {port}: занят {portOwner.ProcessName}";

        return new RuntimeDashboardSnapshot(
            usedGpuGb,
            totalGpuGb,
            IsPortFree: portOwner is null,
            gpuText,
            portText,
            RuntimeText: "runtime: требуется проверка llama.cpp");
    }

    private async Task<IReadOnlyList<GpuInfo>> SafeGetGpusAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await gpuProbe.GetGpusAsync(cancellationToken);
        }
        catch
        {
            return [];
        }
    }
}
