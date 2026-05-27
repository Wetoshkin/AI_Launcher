using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimeCatalogServiceTests
{
    [Fact]
    public async Task FindsLlamaServerAndParsesCapabilities()
    {
        var root = CreateTempRuntimeRoot();
        var executable = Path.Combine(root, "llama-server.exe");
        File.WriteAllText(executable, "fake");
        var runner = new FakeCommandRunner("""
        --spec-type TYPE    none, draft, draft-mtp
        --cache-type-k TYPE f16, q8_0, turbo3, turbo4
        """);
        var service = new RuntimeCatalogService(runner);

        var runtimes = await service.ScanAsync([root], CancellationToken.None);

        Assert.Single(runtimes);
        Assert.Equal(executable, runtimes[0].ExecutablePath);
        Assert.True(runtimes[0].Capabilities.SupportsMtp);
        Assert.True(runtimes[0].Capabilities.SupportsTurboQuant);
        Assert.Equal(executable, runner.LastFileName);
        Assert.Equal("--help", runner.LastArguments);
        Directory.Delete(root, recursive: true);
    }

    private static string CreateTempRuntimeRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"launcher-runtime-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
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
