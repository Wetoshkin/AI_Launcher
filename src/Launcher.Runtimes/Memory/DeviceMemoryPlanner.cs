using Launcher.Runtimes.Hardware;

namespace Launcher.Runtimes.Memory;

/// <summary>
/// Раскладывает модель (веса + KV + overhead) по видеокартам и системной RAM.
/// GPU заполняются в первую очередь (по доле выгрузки), остаток уходит в RAM.
/// То, что не помещается даже в RAM — это <see cref="DeviceMemoryPlan.OverflowGb"/>.
/// </summary>
public static class DeviceMemoryPlanner
{
    public static DeviceMemoryPlan Plan(
        MemoryEstimate estimate,
        SystemHardware hardware,
        double gpuOffloadFraction = 1.0)
    {
        var totalModelGb = Math.Max(0.0, estimate.TotalGb);
        var offload = Math.Clamp(gpuOffloadFraction, 0.0, 1.0);
        var gpuRequestGb = totalModelGb * offload;

        var forecast = GpuMemoryForecaster.Forecast(hardware.Gpus, gpuRequestGb);

        var devices = new List<DeviceMemoryRow>(hardware.Gpus.Count + 1);
        foreach (var row in forecast.Rows)
        {
            devices.Add(new DeviceMemoryRow(
                row.Gpu.Name,
                MemoryDeviceKind.Gpu,
                row.Gpu.TotalGb,
                row.Gpu.UsedGb,
                row.AddedGb));
        }

        // Остаток модели для RAM = то, что не отдали на GPU + то, что не влезло в GPU.
        var ramModelGb = (totalModelGb - gpuRequestGb) + forecast.OverflowGb;
        var ramBaseUsedGb = Math.Max(0.0, hardware.RamTotalGb - hardware.RamFreeGb);

        var overflowGb = Math.Max(0.0, ramModelGb - hardware.RamFreeGb);

        if (ramModelGb > 0.01 || hardware.Gpus.Count == 0)
        {
            devices.Add(new DeviceMemoryRow(
                "Системная RAM",
                MemoryDeviceKind.SystemRam,
                hardware.RamTotalGb,
                ramBaseUsedGb,
                ramModelGb));
        }

        return new DeviceMemoryPlan(devices, totalModelGb, overflowGb);
    }
}
