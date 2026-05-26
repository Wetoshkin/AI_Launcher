using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Tests;

public sealed class GpuMemoryTests
{
    [Fact]
    public void NvidiaSmiParserSupportsPlainNumbersAndMibSuffix()
    {
        var gpus = NvidiaSmiParser.ParseGpuRows(
            """
            GeForce GTX 1080 Ti, 1024, 11264
            Tesla P40, 512 MiB, 24576 MiB
            """);

        Assert.Equal(["GeForce GTX 1080 Ti", "Tesla P40"], gpus.Select(gpu => gpu.Name));
        Assert.Equal(1.0, gpus[0].UsedGb, 3);
        Assert.Equal(11.0, gpus[0].TotalGb, 3);
        Assert.Equal(0.5, gpus[1].UsedGb, 3);
        Assert.Equal(24.0, gpus[1].TotalGb, 3);
    }

    [Fact]
    public void ForecastDistributesAdditionalMemoryAcrossFreeGpuCapacity()
    {
        var forecast = GpuMemoryForecaster.Forecast(
            [
                new GpuInfo("GPU0", 22.0, 24.0),
                new GpuInfo("GPU1", 10.0, 12.0)
            ],
            additionalGb: 10.0);

        Assert.Equal([2.0, 2.0], forecast.Rows.Select(row => row.AddedGb));
        Assert.Equal(6.0, forecast.OverflowGb);
        Assert.Equal(24.0, forecast.Rows[0].ProjectedUsedGb);
        Assert.Equal(0.0, forecast.Rows[0].FreeAfterGb);
    }

    [Fact]
    public async Task NvidiaSmiGpuProbeRunsExpectedQueryAndParsesOutput()
    {
        var runner = new FakeCommandRunner("RTX 3090, 2048, 24576");
        var probe = new NvidiaSmiGpuProbe(runner);

        var gpus = await probe.GetGpusAsync(CancellationToken.None);

        Assert.Single(gpus);
        Assert.Equal("nvidia-smi", runner.LastFileName);
        Assert.Contains("--query-gpu=name,memory.used,memory.total", runner.LastArguments);
        Assert.Equal("RTX 3090", gpus[0].Name);
        Assert.Equal(2.0, gpus[0].UsedGb, 3);
        Assert.Equal(24.0, gpus[0].TotalGb, 3);
    }

    private sealed class FakeCommandRunner(string output) : ICommandRunner
    {
        public string LastFileName { get; private set; } = "";
        public string LastArguments { get; private set; } = "";

        public Task<string> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            LastFileName = fileName;
            LastArguments = arguments;
            return Task.FromResult(output);
        }
    }
}
