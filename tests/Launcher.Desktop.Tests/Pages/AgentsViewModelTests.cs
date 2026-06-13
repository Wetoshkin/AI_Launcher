using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Agents.Discovery;
using Launcher.Core.Scenarios;
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests.Pages;

public class AgentsViewModelTests
{
    private sealed class NotInstalledResolver : IExecutableResolver
    {
        public Task<string?> FindExecutableAsync(string n, CancellationToken c) => Task.FromResult<string?>(null);
        public Task<string?> GetVersionAsync(string n, CancellationToken c) => Task.FromResult<string?>(null);
    }

    [Fact]
    public async Task Check_lists_all_agents_as_not_installed()
    {
        var vm = new AgentsViewModel(new AgentCliCatalogService(new NotInstalledResolver()));

        await vm.CheckAgentsCommand.ExecuteAsync(null);

        Assert.Equal(6, vm.Agents.Count);
        Assert.All(vm.Agents, a => Assert.False(a.Installed));
    }

    [Fact]
    public async Task Prepare_writes_opencode_config_into_project()
    {
        var temp = Path.Combine(Path.GetTempPath(), "als-agent-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var vm = new AgentsViewModel(new AgentCliCatalogService(new NotInstalledResolver()))
            {
                ProjectFolder = temp,
                SelectedAgent = AgentKind.OpenCode,
                Model = "local/test-model",
                BaseUrl = "http://127.0.0.1:8080/v1",
            };

            await vm.PrepareAndLaunchCommand.ExecuteAsync(null);

            Assert.True(File.Exists(Path.Combine(temp, "opencode.json")));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }
}
