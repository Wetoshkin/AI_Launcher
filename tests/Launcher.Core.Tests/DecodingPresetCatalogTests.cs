using Launcher.Core.Decoding;

namespace Launcher.Core.Tests;

public sealed class DecodingPresetCatalogTests
{
    [Fact]
    public void SafeCodingPresetKeepsEosAndHasLowLoopRisk()
    {
        var preset = DecodingPresetCatalog.Get("coding-safe");

        Assert.Equal("Безопасный coding", preset.Name);
        Assert.False(preset.IgnoreEos);
        Assert.False(preset.EnableMtp);
        Assert.Equal(LoopRiskLevel.Low, preset.LoopRisk);
        Assert.Contains("--repeat-penalty", preset.Arguments.Keys);
    }

    [Fact]
    public void MtpFastPresetDocumentsMediumLoopRisk()
    {
        var preset = DecodingPresetCatalog.Get("mtp-fast");

        Assert.True(preset.EnableMtp);
        Assert.Equal("draft-mtp", preset.SpecType);
        Assert.Equal(LoopRiskLevel.Medium, preset.LoopRisk);
        Assert.Contains("--spec-draft-n-max", preset.Arguments.Keys);
    }
}
