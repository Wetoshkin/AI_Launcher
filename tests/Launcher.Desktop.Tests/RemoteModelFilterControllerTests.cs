using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.Tests;

public sealed class RemoteModelFilterControllerTests
{
    [Fact]
    public void ApplyModelsCombinesFamilyQuantSizeAndCapabilityFilters()
    {
        var models = new[]
        {
            Model(
                "unsloth/Qwen3-Coder-GGUF",
                ["gguf", "tool-calling", "qwen"],
                [new("Qwen3-Coder-Q4_K_M.gguf", 4L * 1024 * 1024 * 1024)]),
            Model(
                "unsloth/Qwen3-Coder-Q8-GGUF",
                ["gguf", "qwen"],
                [new("Qwen3-Coder-Q8_0.gguf", 12L * 1024 * 1024 * 1024)]),
            Model(
                "unsloth/DeepSeek-Coder-GGUF",
                ["gguf", "tool-calling", "deepseek"],
                [new("DeepSeek-Coder-Q4_K_M.gguf", 4L * 1024 * 1024 * 1024)])
        };

        var filtered = RemoteModelFilterController.ApplyModels(
            models,
            familyFilter: "Qwen",
            quantFilter: "Q4_K_M",
            sizeFilter: "до 8 ГБ",
            capabilityFilter: HuggingFaceCapabilityFilter.Tools);

        var model = Assert.Single(filtered);
        Assert.Equal("unsloth/Qwen3-Coder-GGUF", model.Id);
    }

    [Fact]
    public void MatchesSizeAcceptsUnknownSizeOnlyWhenUnknownFilterSelected()
    {
        var option = new RemoteGgufDownloadOptionRowViewModel(
            "unsloth/Qwen3-Coder-GGUF",
            new HuggingFaceGgufDownloadOption(
                "Qwen3-Coder-Q4_K_M.gguf",
                "Q4_K_M",
                IsSplit: false,
                [new HuggingFaceGgufFile("Qwen3-Coder-Q4_K_M.gguf", "https://example.test/model.gguf", true)]));

        Assert.True(RemoteModelFilterController.MatchesSize(option, "любой размер"));
        Assert.True(RemoteModelFilterController.MatchesSize(option, "неизвестный"));
        Assert.False(RemoteModelFilterController.MatchesSize(option, "до 8 ГБ"));
    }

    private static HuggingFaceModelSummary Model(
        string id,
        IReadOnlyList<string> tags,
        IReadOnlyList<FileSpec> files) =>
        new(
            id,
            Downloads: 1,
            Likes: 1,
            Tags: tags,
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFiles: files.Select(file => file.Name).ToArray(),
            SiblingFileMetadata: files
                .Select(file => new HuggingFaceSiblingFile(file.Name, file.SizeBytes))
                .ToArray());

    private sealed record FileSpec(string Name, long? SizeBytes);
}
