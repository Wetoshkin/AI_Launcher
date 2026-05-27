using Launcher.Core.LaunchPlans;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;
using Launcher.Runtimes.Startup;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimeStartCoordinatorTests
{
    [Fact]
    public async Task ReleasesLlamaServerPortBeforeStarting()
    {
        var inspector = new FakePortInspector(new PortOwnerInfo(8080, 1234, "llama-server", null, false, null));
        var releaser = new FakePortReleaser(new PortReleaseResult(true, "released"));
        var starter = new FakeProcessStarter();
        var coordinator = new RuntimeStartCoordinator(inspector, releaser, starter);

        var result = await coordinator.StartAsync(Plan(), 8080, null, CancellationToken.None);

        Assert.True(releaser.Called);
        Assert.Equal(99, result.ProcessId);
        Assert.Equal("released", result.Messages[0]);
    }

    [Fact]
    public async Task RefusesUnknownPortOwner()
    {
        var inspector = new FakePortInspector(new PortOwnerInfo(8080, 2345, "postgres", null, false, null));
        var coordinator = new RuntimeStartCoordinator(
            inspector,
            new FakePortReleaser(new PortReleaseResult(false, "refused")),
            new FakeProcessStarter());

        var result = await coordinator.StartAsync(Plan(), 8080, null, CancellationToken.None);

        Assert.False(result.Started);
        Assert.Contains("postgres", result.Messages[0]);
    }

    private static LaunchPlan Plan() => new("llama-server", new[] { "--port", "8080" }, new Dictionary<string, string>());

    private sealed class FakePortInspector(PortOwnerInfo? owner) : IPortInspector
    {
        public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken) => Task.FromResult(owner);
    }

    private sealed class FakePortReleaser(PortReleaseResult result) : IPortReleaser
    {
        public bool Called { get; private set; }

        public Task<PortReleaseResult> ReleaseIfSafeAsync(PortOwnerInfo portOwner, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeProcessStarter : IProcessStarter
    {
        public Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new ProcessStartResult(99));
    }
}
