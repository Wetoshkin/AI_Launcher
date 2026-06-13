using Launcher.Desktop.Services;
using Xunit;

namespace Launcher.Desktop.Tests.Services;

public class HfReadmeParserTests
{
    [Fact]
    public void Extracts_sampler_flags_from_command_drops_infra()
    {
        var readme = "Run it:\n```\nllama-server -m model.gguf -ngl 99 -c 4096 --temp 0.6 --top-k 20 --top-p 0.95 --port 8080\n```";
        var args = HfReadmeParser.ExtractRecommendedArgs(readme);

        Assert.NotNull(args);
        Assert.Contains("--temp 0.6", args);
        Assert.Contains("--top-k 20", args);
        Assert.Contains("--top-p 0.95", args);
        Assert.DoesNotContain("-m ", args);
        Assert.DoesNotContain("-ngl", args);
        Assert.DoesNotContain("--ctx-size", args);
        Assert.DoesNotContain("--port", args);
    }

    [Fact]
    public void Extracts_from_prose()
    {
        var readme = "We recommend temperature = 0.7 and top_p of 0.9 for best results.";
        var args = HfReadmeParser.ExtractRecommendedArgs(readme);

        Assert.NotNull(args);
        Assert.Contains("--temp 0.7", args);
        Assert.Contains("--top-p 0.9", args);
    }

    [Fact]
    public void Returns_null_when_nothing_relevant()
    {
        Assert.Null(HfReadmeParser.ExtractRecommendedArgs("Just a description of the model with no params."));
        Assert.Null(HfReadmeParser.ExtractRecommendedArgs(null));
    }
}
