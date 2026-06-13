using Launcher.Agents.Discovery;
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Tests;

public sealed class AgentCliCatalogServiceTests
{
    [Fact]
    public async Task CheckAsyncReportsInstalledAndMissingAgentExecutables()
    {
        var resolver = new FakeExecutableResolver(
            new Dictionary<string, string?>
            {
                ["opencode"] = @"C:\tools\opencode.cmd",
                ["crush"] = null,
                ["aider"] = null,
                ["goose"] = null,
                ["kilo"] = null,
                ["pi"] = null
            },
            new Dictionary<string, string>
            {
                ["opencode"] = "opencode 1.2.3"
            });
        var catalog = new AgentCliCatalogService(resolver);

        var statuses = await catalog.CheckAsync(CancellationToken.None);

        Assert.Collection(statuses,
            status =>
            {
                Assert.Equal(AgentKind.OpenCode, status.Agent);
                Assert.True(status.IsInstalled);
                Assert.Equal("opencode", status.ExecutableName);
                Assert.Equal(@"C:\tools\opencode.cmd", status.ExecutablePath);
                Assert.Equal("opencode 1.2.3", status.VersionText);
            },
            status => Assert.Equal(AgentKind.Crush, status.Agent),
            status =>
            {
                Assert.Equal(AgentKind.Aider, status.Agent);
                Assert.False(status.IsInstalled);
                Assert.Equal("aider", status.ExecutableName);
                Assert.Equal("не найден", status.StatusText);
            },
            status => Assert.Equal(AgentKind.Goose, status.Agent),
            status => Assert.Equal(AgentKind.Kilo, status.Agent),
            status => Assert.Equal(AgentKind.Pi, status.Agent));
    }

    [Fact]
    public async Task CheckAsyncCanCheckSingleAgent()
    {
        var resolver = new FakeExecutableResolver(
            new Dictionary<string, string?> { ["aider"] = @"C:\Python\Scripts\aider.exe" },
            new Dictionary<string, string> { ["aider"] = "aider 0.80.0" });
        var catalog = new AgentCliCatalogService(resolver);

        var status = await catalog.CheckAsync(AgentKind.Aider, CancellationToken.None);

        Assert.True(status.IsInstalled);
        Assert.Equal("aider", status.ExecutableName);
        Assert.Equal(@"C:\Python\Scripts\aider.exe", status.ExecutablePath);
    }

    private sealed class FakeExecutableResolver(
        IReadOnlyDictionary<string, string?> paths,
        IReadOnlyDictionary<string, string> versions) : IExecutableResolver
    {
        public Task<string?> FindExecutableAsync(string executableName, CancellationToken cancellationToken) =>
            Task.FromResult(paths.TryGetValue(executableName, out var path) ? path : null);

        public Task<string?> GetVersionAsync(string executableName, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(versions.TryGetValue(executableName, out var version) ? version : null);
    }
}
