using Launcher.Agents.Commands;
using Launcher.Core.Scenarios;

namespace Launcher.Agents.Tests;

public sealed class AgentProjectConfigWriterTests
{
    [Fact]
    public async Task WritesKiloJsoncForOpenAiCompatibleLocalEndpoint()
    {
        using var temp = new TempDirectory();
        var writer = new AgentProjectConfigWriter();

        var result = await writer.WriteAsync(
            new AgentLaunchRequest(
                AgentKind.Kilo,
                temp.Path,
                "local/llama.cpp/model",
                "http://127.0.0.1:8080/v1"),
            CancellationToken.None);

        var file = Path.Combine(temp.Path, "kilo.jsonc");
        Assert.Equal(file, result.ConfigPath);
        var text = await File.ReadAllTextAsync(file);
        Assert.Contains(@"""baseUrl"": ""http://127.0.0.1:8080/v1""", text);
        Assert.Contains(@"""model"": ""local/llama.cpp/model""", text);
        Assert.Contains(@"""tools"": false", text);
    }

    [Fact]
    public async Task WritesOpenCodeJsonForLocalOpenAiProvider()
    {
        using var temp = new TempDirectory();
        var writer = new AgentProjectConfigWriter();

        var result = await writer.WriteAsync(
            new AgentLaunchRequest(
                AgentKind.OpenCode,
                temp.Path,
                "local/llama.cpp/model",
                "http://127.0.0.1:8080/v1"),
            CancellationToken.None);

        var file = Path.Combine(temp.Path, "opencode.json");
        Assert.Equal(file, result.ConfigPath);
        var text = await File.ReadAllTextAsync(file);
        Assert.Contains(@"""baseURL"": ""http://127.0.0.1:8080/v1""", text);
        Assert.Contains(@"""model"": ""local/llama.cpp/model""", text);
        Assert.Contains(@"""disabled"": true", text);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "agent-config-" + Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
