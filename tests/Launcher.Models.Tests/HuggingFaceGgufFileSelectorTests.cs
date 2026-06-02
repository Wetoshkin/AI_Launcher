using Launcher.Models.HuggingFace;

namespace Launcher.Models.Tests;

public sealed class HuggingFaceGgufFileSelectorTests
{
    [Fact]
    public void SelectDownloadOptionsFiltersNonModelFilesAndBuildsResolveUrls()
    {
        var model = new HuggingFaceModelSummary(
            "unsloth/Qwen3-Coder-GGUF",
            Downloads: 1000,
            Likes: 42,
            Tags: ["gguf"],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFiles:
            [
                "Qwen3-Coder-30B-A3B-Q4_K_M.gguf",
                "mmproj-Qwen3-Coder.gguf",
                "README.md",
                "Qwen3 Coder Q5_K_M.gguf"
            ]);

        var options = HuggingFaceGgufFileSelector.SelectDownloadOptions(model);

        Assert.Collection(options,
            option =>
            {
                Assert.Equal("Qwen3-Coder-30B-A3B-Q4_K_M.gguf", option.Label);
                Assert.Equal("Q4_K_M", option.Quant);
                Assert.False(option.IsSplit);
                Assert.Equal("https://huggingface.co/unsloth/Qwen3-Coder-GGUF/resolve/main/Qwen3-Coder-30B-A3B-Q4_K_M.gguf", option.Files.Single().DownloadUrl);
            },
            option =>
            {
                Assert.Equal("Qwen3 Coder Q5_K_M.gguf", option.Label);
                Assert.Equal("Q5_K_M", option.Quant);
                Assert.Equal("https://huggingface.co/unsloth/Qwen3-Coder-GGUF/resolve/main/Qwen3%20Coder%20Q5_K_M.gguf", option.Files.Single().DownloadUrl);
            });
    }

    [Fact]
    public void SelectDownloadOptionsGroupsSplitShardsIntoOneOption()
    {
        var model = new HuggingFaceModelSummary(
            "bartowski/DeepSeek-R1-GGUF",
            Downloads: 5000,
            Likes: 100,
            Tags: ["gguf"],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFiles:
            [
                "DeepSeek-R1-Q4_K_M-00002-of-00003.gguf",
                "DeepSeek-R1-Q4_K_M-00001-of-00003.gguf",
                "DeepSeek-R1-Q4_K_M-00003-of-00003.gguf"
            ]);

        var option = Assert.Single(HuggingFaceGgufFileSelector.SelectDownloadOptions(model));

        Assert.Equal("DeepSeek-R1-Q4_K_M.gguf", option.Label);
        Assert.Equal("Q4_K_M", option.Quant);
        Assert.True(option.IsSplit);
        Assert.Equal(3, option.TotalFiles);
        Assert.Equal("DeepSeek-R1-Q4_K_M-00001-of-00003.gguf", option.PrimaryFile.FileName);
        Assert.True(option.PrimaryFile.IsFirstSplitShard);
        Assert.All(option.Files.Skip(1), file => Assert.False(file.IsFirstSplitShard));
        Assert.Equal(
            [
                "DeepSeek-R1-Q4_K_M-00001-of-00003.gguf",
                "DeepSeek-R1-Q4_K_M-00002-of-00003.gguf",
                "DeepSeek-R1-Q4_K_M-00003-of-00003.gguf"
            ],
            option.Files.Select(file => file.FileName));
    }

    [Fact]
    public void SelectDownloadOptionsCarriesAndFormatsFileSizes()
    {
        var model = new HuggingFaceModelSummary(
            "bartowski/Model-GGUF",
            Downloads: 5000,
            Likes: 100,
            Tags: ["gguf"],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFileMetadata:
            [
                new HuggingFaceSiblingFile("Model-Q4_K_M.gguf", 4_294_967_296),
                new HuggingFaceSiblingFile("Model-Q5_K_M-00002-of-00002.gguf", 1_073_741_824),
                new HuggingFaceSiblingFile("Model-Q5_K_M-00001-of-00002.gguf", 2_147_483_648),
                new HuggingFaceSiblingFile("Model-Q8_0.gguf", null)
            ]);

        var options = HuggingFaceGgufFileSelector.SelectDownloadOptions(model);

        Assert.Collection(options,
            option =>
            {
                Assert.Equal("Model-Q4_K_M.gguf", option.Label);
                Assert.Equal(4_294_967_296, option.TotalSizeBytes);
                Assert.Equal("4 GB", option.FormattedSize);
                var file = option.Files.Single();
                Assert.Equal(4_294_967_296, file.SizeBytes);
                Assert.Equal("4 GB", file.FormattedSize);
            },
            option =>
            {
                Assert.Equal("Model-Q5_K_M.gguf", option.Label);
                Assert.Equal(3_221_225_472, option.TotalSizeBytes);
                Assert.Equal("3 GB", option.FormattedSize);
                Assert.Equal(["2 GB", "1 GB"], option.Files.Select(file => file.FormattedSize));
            },
            option =>
            {
                Assert.Equal("Model-Q8_0.gguf", option.Label);
                Assert.Null(option.TotalSizeBytes);
                Assert.Equal("", option.FormattedSize);
                Assert.Null(option.Files.Single().SizeBytes);
                Assert.Equal("", option.Files.Single().FormattedSize);
            });
    }
}
