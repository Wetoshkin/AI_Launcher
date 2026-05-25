using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Tests;

public sealed class WindowsPortInspectorTests
{
    [Fact]
    public async Task UsesCommandRunnerAndParsesPortOwner()
    {
        var runner = new FakeCommandRunner("""
        {"LocalPort":8080,"OwningProcess":1234,"ProcessName":"llama-server","Path":"D:\\AI\\llama-server.exe"}
        """);
        var inspector = new WindowsPortInspector(runner);

        var info = await inspector.InspectAsync(8080, CancellationToken.None);

        Assert.NotNull(info);
        Assert.Equal(8080, info.Port);
        Assert.Equal(1234, info.ProcessId);
        Assert.True(info.IsLikelyLlamaServer);
        Assert.Contains("Get-NetTCPConnection", runner.LastArguments);
    }

    private sealed class FakeCommandRunner(string output) : ICommandRunner
    {
        public string LastArguments { get; private set; } = "";

        public Task<string> RunAsync(string fileName, string arguments, CancellationToken cancellationToken)
        {
            LastArguments = arguments;
            return Task.FromResult(output);
        }
    }
}
