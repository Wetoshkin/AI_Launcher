using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Memory;

namespace Launcher.Runtimes.Tests.Memory;

public class DeviceMemoryPlannerTests
{
    private static MemoryEstimate Estimate(double totalGb) =>
        new(totalGb, 0.0, 8.5, 0.0, totalGb);

    [Fact]
    public void Model_fits_entirely_on_a_large_gpu()
    {
        var hw = new SystemHardware("CPU", new[] { new GpuInfo("RTX 4090", 0.0, 24.0) }, 32.0, 24.0);

        var plan = DeviceMemoryPlanner.Plan(Estimate(8.0), hw);

        var gpu = plan.Devices.Single(d => d.Kind == MemoryDeviceKind.Gpu);
        Assert.Equal(8.0, gpu.ModelGb, 1);
        Assert.True(plan.Fits);
        Assert.Equal(0.0, plan.OverflowGb, 1);
    }

    [Fact]
    public void Model_spills_from_small_igpu_into_system_ram()
    {
        var hw = new SystemHardware("Ultra 7", new[] { new GpuInfo("Intel Arc", 0.0, 2.0) }, 31.5, 16.0);

        var plan = DeviceMemoryPlanner.Plan(Estimate(8.0), hw);

        var gpu = plan.Devices.Single(d => d.Kind == MemoryDeviceKind.Gpu);
        var ram = plan.Devices.Single(d => d.Kind == MemoryDeviceKind.SystemRam);
        Assert.Equal(2.0, gpu.ModelGb, 1);
        Assert.Equal(6.0, ram.ModelGb, 1);
        Assert.True(plan.Fits);
    }

    [Fact]
    public void Model_too_big_overflows_everywhere()
    {
        var hw = new SystemHardware("Ultra 7", new[] { new GpuInfo("Intel Arc", 0.0, 2.0) }, 31.5, 16.0);

        var plan = DeviceMemoryPlanner.Plan(Estimate(40.0), hw);

        Assert.False(plan.Fits);
        Assert.Equal(22.0, plan.OverflowGb, 1);
        Assert.True(plan.Devices.Single(d => d.Kind == MemoryDeviceKind.SystemRam).IsOverflowing);
    }

    [Fact]
    public void Cpu_only_machine_places_everything_in_ram()
    {
        var hw = new SystemHardware("Ultra 7", Array.Empty<GpuInfo>(), 31.5, 16.0);

        var plan = DeviceMemoryPlanner.Plan(Estimate(8.0), hw);

        Assert.Single(plan.Devices);
        Assert.Equal(MemoryDeviceKind.SystemRam, plan.Devices[0].Kind);
        Assert.Equal(8.0, plan.Devices[0].ModelGb, 1);
        Assert.True(plan.Fits);
    }
}
