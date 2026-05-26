namespace Launcher.Runtimes.Hardware;

public static class GpuMemoryForecaster
{
    public static GpuMemoryForecast Forecast(IReadOnlyList<GpuInfo> gpus, double additionalGb)
    {
        var remainingGb = Math.Max(0.0, additionalGb);
        var rows = new List<GpuMemoryForecastRow>(gpus.Count);

        foreach (var gpu in gpus)
        {
            var addedGb = Math.Min(remainingGb, gpu.FreeGb);
            var projectedUsedGb = gpu.UsedGb + addedGb;
            var freeAfterGb = Math.Max(0.0, gpu.TotalGb - projectedUsedGb);

            rows.Add(new GpuMemoryForecastRow(gpu, addedGb, projectedUsedGb, freeAfterGb));
            remainingGb -= addedGb;
        }

        return new GpuMemoryForecast(rows, Math.Max(0.0, remainingGb));
    }
}
