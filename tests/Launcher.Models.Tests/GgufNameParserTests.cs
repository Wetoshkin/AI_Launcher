using Launcher.Models.Catalog;

namespace Launcher.Models.Tests;

public sealed class GgufNameParserTests
{
    [Fact]
    public void ParsesFamilySizeAndQuantFromCommonGgufName()
    {
        var model = GgufNameParser.Parse(@"D:\AI\Models\Qwen3-Coder-30B-A3B-Instruct-Q4_K_M.gguf");

        Assert.Equal("Qwen", model.Family);
        Assert.Equal("30B", model.SizeLabel);
        Assert.Equal("Q4_K_M", model.Quant);
    }
}
