using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Tests;

public sealed class PortReleaseServiceTests
{
    [Fact]
    public async Task ReleasesLikelyLlamaServerWithStopProcessCommand()
    {
        var runner = new FakeCommandRunner();
        var service = new PortReleaseService(runner);

        var result = await service.ReleaseIfSafeAsync(
            new PortOwnerInfo(8080, 1234, "llama-server", @"D:\AI\runtimes\llama-server.exe", true, "qwen"),
            CancellationToken.None);

        Assert.True(result.Released);
        Assert.Contains("Stop-Process -Id 1234", runner.LastArguments);
    }

    [Fact]
    public async Task RefusesToReleaseUnknownProcess()
    {
        var runner = new FakeCommandRunner();
        var service = new PortReleaseService(runner);

        var result = await service.ReleaseIfSafeAsync(
            new PortOwnerInfo(8080, 4321, "postgres", @"C:\Postgres\postgres.exe", true, null),
            CancellationToken.None);

        Assert.False(result.Released);
        Assert.Contains("postgres", result.Message);
        Assert.Equal("", runner.LastArguments);
    }

    private sealed class FakeCommandRunner : ICommandRunner
    {
        public string LastArguments { get; private set; } = "";

        public Task<string> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            LastArguments = arguments;
            return Task.FromResult("");
        }
    }
}
