using Launcher.Desktop.Services;
using Xunit;

namespace Launcher.Desktop.Tests.Services;

public class ModelFitTests
{
    [Fact]
    public void Small_model_fits_vram_fast()
    {
        var (_, level) = ModelFit.Describe(sizeGb: 4.0, vramGb: 24.0, ramGb: 64.0);
        Assert.Equal(0, level);
    }

    [Fact]
    public void Mid_model_offloads_to_ram()
    {
        var (_, level) = ModelFit.Describe(sizeGb: 30.0, vramGb: 24.0, ramGb: 64.0);
        Assert.Equal(1, level);
    }

    [Fact]
    public void Huge_model_does_not_fit()
    {
        var (_, level) = ModelFit.Describe(sizeGb: 200.0, vramGb: 24.0, ramGb: 64.0);
        Assert.Equal(2, level);
    }

    [Fact]
    public void Unknown_size_is_reported()
    {
        var (_, level) = ModelFit.Describe(sizeGb: 0, vramGb: 24.0, ramGb: 64.0);
        Assert.Equal(3, level);
    }
}
