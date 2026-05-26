using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Status;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimeDashboardServiceTests
{
    [Fact]
    public async Task ReportsGpuTotalsAndFreePort()
    {
        var service = new RuntimeDashboardService(
            new FakeGpuProbe([new GpuInfo("RTX 3090", 10.0, 24.0)]),
            new FakePortInspector(null));

        var snapshot = await service.CheckAsync(8080, CancellationToken.None);

        Assert.Equal(10.0, snapshot.UsedGpuGb);
        Assert.Equal(24.0, snapshot.TotalGpuGb);
        Assert.True(snapshot.IsPortFree);
        Assert.Equal("порт 8080: свободен", snapshot.PortText);
        Assert.Equal("GPU: 10.0 / 24.0 ГБ", snapshot.GpuText);
    }

    [Fact]
    public async Task ReportsOccupiedPortOwner()
    {
        var service = new RuntimeDashboardService(
            new FakeGpuProbe([]),
            new FakePortInspector(new PortOwnerInfo(8080, 1234, "llama-server", null, false, null)));

        var snapshot = await service.CheckAsync(8080, CancellationToken.None);

        Assert.False(snapshot.IsPortFree);
        Assert.Equal("порт 8080: занят llama-server", snapshot.PortText);
    }

    private sealed class FakeGpuProbe(IReadOnlyList<GpuInfo> gpus) : IGpuProbe
    {
        public Task<IReadOnlyList<GpuInfo>> GetGpusAsync(CancellationToken cancellationToken) =>
            Task.FromResult(gpus);
    }

    private sealed class FakePortInspector(PortOwnerInfo? owner) : IPortInspector
    {
        public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken) =>
            Task.FromResult(owner);
    }
}
