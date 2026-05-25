using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Tests;

public sealed class PortOwnerInfoTests
{
    [Fact]
    public void ClassifiesOwnLlamaServerByExecutableName()
    {
        var info = new PortOwnerInfo(8080, 1234, "llama-server.exe", @"D:\AI\runtimes\llama-server.exe", true, "qwen");

        Assert.True(info.IsLikelyLlamaServer);
        Assert.True(info.EndpointResponds);
        Assert.Equal("qwen", info.LoadedModelId);
    }
}
