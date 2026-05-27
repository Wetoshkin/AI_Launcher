using Launcher.Core.LaunchPlans;
using Launcher.Runtimes.Processes;

namespace Launcher.Runtimes.Tests;

public sealed class LaunchPlanProcessServiceTests
{
    [Fact]
    public async Task DelegatesLaunchPlanToProcessStarterWithEnvironment()
    {
        var starter = new FakeProcessStarter();
        var service = new LaunchPlanProcessService(starter);
        var plan = new LaunchPlan(
            "tool",
            new[] { "--flag", "value" },
            new Dictionary<string, string> { ["OPENAI_API_KEY"] = "local" });

        var result = await service.StartAsync(plan, @"D:\AI\Projects\App", CancellationToken.None);

        Assert.Equal(42, result.ProcessId);
        Assert.Equal("tool", starter.Request!.Executable);
        Assert.Equal(@"D:\AI\Projects\App", starter.Request.WorkingDirectory);
        Assert.Contains("--flag", starter.Request.Arguments);
        Assert.Equal("local", starter.Request.Environment["OPENAI_API_KEY"]);
    }

    private sealed class FakeProcessStarter : IProcessStarter
    {
        public ProcessStartRequest? Request { get; private set; }

        public Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new ProcessStartResult(42));
        }
    }
}
