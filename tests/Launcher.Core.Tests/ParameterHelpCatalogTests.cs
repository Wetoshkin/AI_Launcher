using Launcher.Core.Parameters;

namespace Launcher.Core.Tests;

public sealed class ParameterHelpCatalogTests
{
    [Theory]
    [InlineData("context", "Контекст")]
    [InlineData("mtp", "MTP")]
    [InlineData("ignore-eos", "--ignore-eos")]
    public void ProvidesRussianHelpForRequiredParameters(string id, string expectedName)
    {
        var help = ParameterHelpCatalog.Get(id);

        Assert.Equal(expectedName, help.DisplayName);
        Assert.False(string.IsNullOrWhiteSpace(help.ShortText));
        Assert.False(string.IsNullOrWhiteSpace(help.Details));
    }

    [Fact]
    public void MarksIgnoreEosAsDangerous()
    {
        Assert.Equal(ParameterRiskLevel.Danger, ParameterHelpCatalog.Get("ignore-eos").Risk);
    }
}
