using Launcher.Core.Parameters;

namespace Launcher.Core.Tests.Parameters;

public class ParameterHelpCatalogTests
{
    // Каждый настраиваемый параметр интерфейса обязан иметь подсказку «?».
    private static readonly string[] RequiredIds =
    [
        "context", "kv-type-k", "kv-type-v", "flash-attn", "mtp", "mtp-draft-tokens",
        "speculative", "draft-model", "ngl", "ncmoe", "batch", "ubatch", "threads",
        "port", "host", "temperature", "top-k", "top-p", "min-p", "repeat-penalty",
        "presence-penalty", "frequency-penalty", "max-tokens", "reasoning",
        "reasoning-budget", "mmap", "mlock", "cont-batching", "parallel", "alias",
        "api-key", "ignore-eos"
    ];

    [Theory]
    [MemberData(nameof(RequiredIdsData))]
    public void Every_required_parameter_has_help(string id)
    {
        Assert.True(ParameterHelpCatalog.Contains(id), $"Нет подсказки для параметра '{id}'.");

        var help = ParameterHelpCatalog.Get(id);
        Assert.False(string.IsNullOrWhiteSpace(help.DisplayName));
        Assert.False(string.IsNullOrWhiteSpace(help.ShortText));
        Assert.False(string.IsNullOrWhiteSpace(help.Details));
    }

    public static IEnumerable<object[]> RequiredIdsData() => RequiredIds.Select(id => new object[] { id });

    [Fact]
    public void TryGet_returns_false_for_unknown()
    {
        Assert.False(ParameterHelpCatalog.TryGet("does-not-exist", out _));
    }

    [Fact]
    public void Marks_ignore_eos_as_dangerous()
    {
        Assert.Equal(ParameterRiskLevel.Danger, ParameterHelpCatalog.Get("ignore-eos").Risk);
    }
}
