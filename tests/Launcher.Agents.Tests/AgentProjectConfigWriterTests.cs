using System.Text.Json.Nodes;
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
        Assert.False(File.Exists(Path.Combine(temp.Path, "kilocode.json")));

        var root = await ReadJsonAsync(file);
        var provider = Assert.IsType<JsonObject>(root["provider"]);
        Assert.Equal("openai-compatible", provider["type"]?.GetValue<string>());
        Assert.Equal("http://127.0.0.1:8080/v1", provider["baseUrl"]?.GetValue<string>());
        Assert.Equal("local", provider["apiKey"]?.GetValue<string>());
        Assert.Equal("local/llama.cpp/model", provider["model"]?.GetValue<string>());
        Assert.False(provider["tools"]?.GetValue<bool>());
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
        var root = await ReadJsonAsync(file);
        Assert.Equal("local/llama.cpp/model", root["model"]?.GetValue<string>());

        var provider = Assert.IsType<JsonObject>(root["provider"]?["local"]);
        Assert.Equal("@ai-sdk/openai-compatible", provider["npm"]?.GetValue<string>());
        Assert.Equal("local", provider["name"]?.GetValue<string>());

        var options = Assert.IsType<JsonObject>(provider["options"]);
        Assert.Equal("http://127.0.0.1:8080/v1", options["baseURL"]?.GetValue<string>());
        Assert.Equal("local", options["apiKey"]?.GetValue<string>());

        Assert.Null(provider["models"]?["local/llama.cpp/model"]);
        var model = Assert.IsType<JsonObject>(provider["models"]?["llama.cpp/model"]);
        Assert.True(model["tools"]?["disabled"]?.GetValue<bool>());
    }

    [Theory]
    [InlineData(AgentKind.Kilo)]
    [InlineData(AgentKind.OpenCode)]
    public async Task LocalProjectConfigWritersRejectNonLocalModelId(AgentKind agent)
    {
        using var temp = new TempDirectory();
        var writer = new AgentProjectConfigWriter();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            writer.WriteAsync(
                new AgentLaunchRequest(
                    agent,
                    temp.Path,
                    "qwen3-coder-q4",
                    "http://127.0.0.1:8080/v1"),
                CancellationToken.None));
    }

    private static async Task<JsonObject> ReadJsonAsync(string file)
    {
        var text = await File.ReadAllTextAsync(file);
        return Assert.IsType<JsonObject>(JsonNode.Parse(text));
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
