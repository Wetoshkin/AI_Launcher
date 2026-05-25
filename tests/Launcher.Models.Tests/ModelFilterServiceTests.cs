using Launcher.Models.Catalog;

namespace Launcher.Models.Tests;

public sealed class ModelFilterServiceTests
{
    [Fact]
    public void FiltersByQuantAndFamily()
    {
        var models = new[]
        {
            new LocalModelFile("a.gguf", "Qwen", "30B", "Q4_K_M", 18),
            new LocalModelFile("b.gguf", "Gemma", "27B", "Q8_0", 30)
        };

        var result = ModelFilterService.Apply(models, new ModelFilter(Family: "Qwen", Quant: "Q4", MaxSizeGb: null));

        Assert.Single(result);
        Assert.Equal("a.gguf", result[0].Path);
    }
}
