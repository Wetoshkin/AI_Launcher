using Launcher.Runtimes.Hardware;

namespace Launcher.Runtimes.Tests.Hardware;

public class HardwareProbeParserTests
{
    [Fact]
    public void Parses_cpu_gpu_and_ram_from_normalized_lines()
    {
        var output = string.Join("\n",
            "CPU|Intel(R) Core(TM) Ultra 7 155H",
            "GPU|Intel(R) Arc(TM) Graphics|2147479552",
            "RAM|33805717504|18000000000");

        var hardware = HardwareProbeParser.Parse(output);

        Assert.Equal("Intel(R) Core(TM) Ultra 7 155H", hardware.CpuName);
        Assert.Single(hardware.Gpus);
        Assert.Equal("Intel(R) Arc(TM) Graphics", hardware.Gpus[0].Name);
        Assert.Equal(2.0, hardware.Gpus[0].TotalGb, 1);
        Assert.Equal(0.0, hardware.Gpus[0].UsedGb);
        Assert.Equal(31.5, hardware.RamTotalGb, 1);
        Assert.Equal(16.8, hardware.RamFreeGb, 1);
    }

    [Fact]
    public void Skips_zero_memory_virtual_displays()
    {
        var output = string.Join("\n",
            "GPU|Honor Virtual Display Device|0",
            "GPU|NVIDIA GeForce RTX 4090|25757220864",
            "RAM|33805717504|18000000000");

        var hardware = HardwareProbeParser.Parse(output);

        Assert.Single(hardware.Gpus);
        Assert.Equal("NVIDIA GeForce RTX 4090", hardware.Gpus[0].Name);
        Assert.Equal(24.0, hardware.Gpus[0].TotalGb, 1);
    }

    [Fact]
    public void Ignores_malformed_lines_and_returns_empty_when_nothing_valid()
    {
        var hardware = HardwareProbeParser.Parse("garbage\n\nGPU|bad");

        Assert.Equal("неизвестно", hardware.CpuName);
        Assert.Empty(hardware.Gpus);
        Assert.Equal(0.0, hardware.RamTotalGb);
    }
}
