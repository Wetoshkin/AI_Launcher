using Launcher.Models.HuggingFace;

namespace Launcher.Models.Tests;

public sealed class HuggingFaceCapabilityFilterTests
{
    [Fact]
    public void MatchesGgufFromTagRepoNameOrModelSibling()
    {
        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(tags: ["gguf"]),
            HuggingFaceCapabilityFilter.Gguf));

        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(id: "bartowski/DeepSeek-R1-GGUF"),
            HuggingFaceCapabilityFilter.Gguf));

        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(siblingFiles: ["Model-Q4_K_M.gguf"]),
            HuggingFaceCapabilityFilter.Gguf));
    }

    [Fact]
    public void DoesNotTreatMmprojOnlyRepoAsGgufModel()
    {
        var model = Model(siblingFiles: ["mmproj-vision.gguf", "README.md"]);

        Assert.False(HuggingFaceCapabilityFilters.Matches(model, HuggingFaceCapabilityFilter.Gguf));
    }

    [Fact]
    public void MatchesVisionFromTagsOrMmprojSibling()
    {
        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(tags: ["vision", "image-text-to-text"]),
            HuggingFaceCapabilityFilter.Vision));

        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(siblingMetadata: [new HuggingFaceSiblingFile("mmproj-Qwen2-VL.gguf", 512)]),
            HuggingFaceCapabilityFilter.Vision));
    }

    [Fact]
    public void MatchesToolsFromToolUseAndFunctionCallingTags()
    {
        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(tags: ["tool-use"]),
            HuggingFaceCapabilityFilter.Tools));

        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(tags: ["function-calling"]),
            HuggingFaceCapabilityFilter.Tools));
    }

    [Fact]
    public void MatchesMtpFromTagsOrSiblingNames()
    {
        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(tags: ["mtp"]),
            HuggingFaceCapabilityFilter.Mtp));

        Assert.True(HuggingFaceCapabilityFilters.Matches(
            Model(siblingFiles: ["Qwen3-Coder-MTP-Q4_K_M.gguf"]),
            HuggingFaceCapabilityFilter.Mtp));
    }

    [Fact]
    public void MatchesRuntimeAndTurboQuantCompatibilityFromExistingFlags()
    {
        var model = Model(
            tags: ["gguf"],
            hasPreferredQuant: true,
            isRuntimeCompatible: true);

        Assert.True(HuggingFaceCapabilityFilters.Matches(model, HuggingFaceCapabilityFilter.RuntimeCompatible));
        Assert.True(HuggingFaceCapabilityFilters.Matches(model, HuggingFaceCapabilityFilter.TurboQuantCompatible));
    }

    [Fact]
    public void TurboQuantCompatibilityRequiresGgufPreferredQuantAndRuntimeCompatibility()
    {
        Assert.False(HuggingFaceCapabilityFilters.Matches(
            Model(tags: ["gguf"], hasPreferredQuant: false, isRuntimeCompatible: true),
            HuggingFaceCapabilityFilter.TurboQuantCompatible));

        Assert.False(HuggingFaceCapabilityFilters.Matches(
            Model(tags: ["gguf"], hasPreferredQuant: true, isRuntimeCompatible: false),
            HuggingFaceCapabilityFilter.TurboQuantCompatible));

        Assert.False(HuggingFaceCapabilityFilters.Matches(
            Model(hasPreferredQuant: true, isRuntimeCompatible: true),
            HuggingFaceCapabilityFilter.TurboQuantCompatible));
    }

    [Fact]
    public void ApplyKeepsOnlyModelsMatchingAllRequestedCapabilities()
    {
        var matching = Model(id: "owner/matching-GGUF", tags: ["tool-use"], hasPreferredQuant: true, isRuntimeCompatible: true);
        var wrongCapability = Model(id: "owner/no-tools-GGUF", hasPreferredQuant: true, isRuntimeCompatible: true);
        var wrongRuntime = Model(id: "owner/no-runtime-GGUF", tags: ["tool-use"], hasPreferredQuant: true);

        var result = HuggingFaceCapabilityFilters.Apply(
            [matching, wrongCapability, wrongRuntime],
            [HuggingFaceCapabilityFilter.Tools, HuggingFaceCapabilityFilter.TurboQuantCompatible]);

        var model = Assert.Single(result);
        Assert.Equal("owner/matching-GGUF", model.Id);
    }

    private static HuggingFaceModelSummary Model(
        string id = "owner/model",
        IReadOnlyList<string>? tags = null,
        bool hasPreferredQuant = false,
        bool isRuntimeCompatible = false,
        IReadOnlyList<string>? siblingFiles = null,
        IReadOnlyList<HuggingFaceSiblingFile>? siblingMetadata = null)
    {
        return new HuggingFaceModelSummary(
            id,
            Downloads: 0,
            Likes: 0,
            Tags: tags ?? [],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: hasPreferredQuant,
            IsRuntimeCompatible: isRuntimeCompatible,
            SiblingFiles: siblingFiles,
            SiblingFileMetadata: siblingMetadata);
    }
}
