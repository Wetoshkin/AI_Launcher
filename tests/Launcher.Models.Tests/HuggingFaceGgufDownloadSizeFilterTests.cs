using Launcher.Models.HuggingFace;

namespace Launcher.Models.Tests;

public sealed class HuggingFaceGgufDownloadSizeFilterTests
{
    [Fact]
    public void ApplyKeepsEveryOptionForAnySizeRange()
    {
        var options = new[]
        {
            Option("Model-Q4_K_M.gguf", 4L * 1024 * 1024 * 1024),
            Option("Model-Q5_K_M.gguf", 8L * 1024 * 1024 * 1024),
            Option("Model-Unknown.gguf", null)
        };

        var result = HuggingFaceGgufDownloadSizeFilter.Apply(options, HuggingFaceGgufDownloadSizeRange.Any);

        Assert.Equal(["Model-Q4_K_M.gguf", "Model-Q5_K_M.gguf", "Model-Unknown.gguf"], result.Select(option => option.Label));
    }

    [Theory]
    [InlineData(HuggingFaceGgufDownloadSizeRange.UpTo4Gb, "Model-3GB.gguf", "Model-4GB.gguf")]
    [InlineData(HuggingFaceGgufDownloadSizeRange.UpTo8Gb, "Model-3GB.gguf", "Model-4GB.gguf", "Model-5GB.gguf", "Model-8GB.gguf")]
    [InlineData(HuggingFaceGgufDownloadSizeRange.Between8And16Gb, "Model-16GB.gguf")]
    [InlineData(HuggingFaceGgufDownloadSizeRange.UpTo16Gb, "Model-3GB.gguf", "Model-4GB.gguf", "Model-5GB.gguf", "Model-8GB.gguf", "Model-16GB.gguf")]
    [InlineData(HuggingFaceGgufDownloadSizeRange.Between16And32Gb, "Model-17GB.gguf")]
    public void ApplyFiltersOptionsByInclusiveUpperBoundAndExcludesUnknownSizes(
        HuggingFaceGgufDownloadSizeRange range,
        params string[] expectedLabels)
    {
        var options = new[]
        {
            Option("Model-3GB.gguf", 3L * 1024 * 1024 * 1024),
            Option("Model-4GB.gguf", 4L * 1024 * 1024 * 1024),
            Option("Model-5GB.gguf", 5L * 1024 * 1024 * 1024),
            Option("Model-8GB.gguf", 8L * 1024 * 1024 * 1024),
            Option("Model-16GB.gguf", 16L * 1024 * 1024 * 1024),
            Option("Model-17GB.gguf", 17L * 1024 * 1024 * 1024),
            Option("Model-33GB.gguf", 33L * 1024 * 1024 * 1024),
            Option("Model-Unknown.gguf", null)
        };

        var result = HuggingFaceGgufDownloadSizeFilter.Apply(options, range);

        Assert.Equal(expectedLabels, result.Select(option => option.Label));
    }

    [Fact]
    public void ApplyFiltersUnknownSizesExplicitly()
    {
        var options = new[]
        {
            Option("Model-Known.gguf", 4L * 1024 * 1024 * 1024),
            Option("Model-Unknown.gguf", null)
        };

        var result = HuggingFaceGgufDownloadSizeFilter.Apply(options, HuggingFaceGgufDownloadSizeRange.Unknown);

        Assert.Equal(["Model-Unknown.gguf"], result.Select(option => option.Label));
    }

    [Fact]
    public void ApplyFiltersOptionsOverThirtyTwoGb()
    {
        var options = new[]
        {
            Option("Model-32GB.gguf", 32L * 1024 * 1024 * 1024),
            Option("Model-33GB.gguf", 33L * 1024 * 1024 * 1024)
        };

        var result = HuggingFaceGgufDownloadSizeFilter.Apply(options, HuggingFaceGgufDownloadSizeRange.Over32Gb);

        Assert.Equal(["Model-33GB.gguf"], result.Select(option => option.Label));
    }

    [Fact]
    public void ApplyFiltersOptionsOverSixteenGb()
    {
        var options = new[]
        {
            Option("Model-Q4_K_M.gguf", 16L * 1024 * 1024 * 1024),
            Option("Model-Q8_0.gguf", 17L * 1024 * 1024 * 1024)
        };

        var result = HuggingFaceGgufDownloadSizeFilter.Apply(options, HuggingFaceGgufDownloadSizeRange.Over16Gb);

        Assert.Equal(["Model-Q8_0.gguf"], result.Select(option => option.Label));
    }

    private static HuggingFaceGgufDownloadOption Option(string label, long? sizeBytes)
    {
        return new HuggingFaceGgufDownloadOption(
            label,
            Quant: null,
            IsSplit: false,
            Files:
            [
                new HuggingFaceGgufFile(label, $"https://hf/{Uri.EscapeDataString(label)}", IsFirstSplitShard: true, sizeBytes)
            ]);
    }
}
