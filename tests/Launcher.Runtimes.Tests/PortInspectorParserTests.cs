using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Tests;

public sealed class PortInspectorParserTests
{
    [Fact]
    public void ParsesPowerShellJsonWithProcessFields()
    {
        const string json = """
        {
          "LocalPort": 8080,
          "OwningProcess": 1234,
          "ProcessName": "llama-server",
          "Path": "D:\\AI\\runtimes\\llama-server.exe"
        }
        """;

        var info = PortInspectorParser.ParsePowerShellJson(json, endpointResponds: true, loadedModelId: "qwen");

        Assert.NotNull(info);
        Assert.Equal(8080, info.Port);
        Assert.Equal(1234, info.ProcessId);
        Assert.Equal("llama-server", info.ProcessName);
        Assert.Equal(@"D:\AI\runtimes\llama-server.exe", info.ExecutablePath);
        Assert.True(info.IsLikelyLlamaServer);
        Assert.True(info.EndpointResponds);
        Assert.Equal("qwen", info.LoadedModelId);
    }

    [Fact]
    public void ReturnsNullForEmptyJson()
    {
        Assert.Null(PortInspectorParser.ParsePowerShellJson("", endpointResponds: false, loadedModelId: null));
    }
}
