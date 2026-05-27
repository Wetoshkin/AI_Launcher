using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Ports;
using System.Globalization;

namespace Launcher.Runtimes.Status;

public sealed class RuntimeDashboardService(
    IGpuProbe gpuProbe,
    IPortInspector portInspector,
    ILlamaRuntimeCatalog? runtimeCatalog = null)
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
        var runtimeText = runtimeCatalog is null
            ? "runtime: требуется проверка llama.cpp"
            : RuntimeText(await runtimeCatalog.ScanAsync(DefaultRuntimeRoots(), cancellationToken));

        return new RuntimeDashboardSnapshot(
            usedGpuGb,
            totalGpuGb,
            IsPortFree: portOwner is null,
            gpuText,
            portText,
            runtimeText);
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

    private static IEnumerable<string> DefaultRuntimeRoots()
    {
        yield return Path.Combine(Environment.CurrentDirectory, "runtimes");
        yield return @"D:\AI\runtimes";
    }

    private static string RuntimeText(IReadOnlyList<LlamaRuntimeInfo> runtimes)
    {
        if (runtimes.Count == 0)
        {
            return "runtime: llama-server не найден";
        }

        var best = runtimes[0].Capabilities;
        if (best.SupportsMtp && best.SupportsTurboQuant)
        {
            return "runtime: MTP + TurboQuant";
        }

        if (best.SupportsMtp)
        {
            return "runtime: MTP";
        }

        if (best.SupportsTurboQuant)
        {
            return "runtime: TurboQuant";
        }

        return "runtime: llama.cpp";
    }
}
