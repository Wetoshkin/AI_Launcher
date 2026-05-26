using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class LlamaServerHelpParserTests
{
    [Fact]
    public void DetectsTurboQuantCacheTypesAndMtpFlags()
    {
        const string help = """
        usage: llama-server [options]
          -ctk, --cache-type-k TYPE    KV cache type: f16, q8_0, turbo3, turbo4
          -ctv, --cache-type-v TYPE    KV cache type: f16, q8_0, turbo3, turbo4
          --spec-type TYPE             speculative decoding type: none, draft, draft-mtp
          --spec-draft-n-max N         max draft tokens
          --hf-repo REPO               Hugging Face repo
        """;

        var capabilities = LlamaServerHelpParser.Parse(help);

        Assert.True(capabilities.SupportsTurboQuant);
        Assert.True(capabilities.SupportsMtp);
        Assert.Contains("turbo3", capabilities.CacheTypes);
        Assert.Contains("turbo4", capabilities.CacheTypes);
        Assert.Contains("draft-mtp", capabilities.SpecTypes);
        Assert.True(capabilities.SupportsFlag("-ctk"));
        Assert.True(capabilities.SupportsFlag("--cache-type-k"));
        Assert.Contains("--spec-draft-n-max", capabilities.Flags);
        Assert.Contains("--hf-repo", capabilities.Flags);
    }

    [Fact]
    public void DoesNotReportTurboQuantForPlainCacheTypes()
    {
        const string help = """
        usage: llama-server [options]
          --cache-type-k TYPE    KV cache type: f16, q8_0
          --cache-type-v TYPE    KV cache type: f16, q8_0
        """;

        var capabilities = LlamaServerHelpParser.Parse(help);

        Assert.False(capabilities.SupportsTurboQuant);
        Assert.False(capabilities.SupportsMtp);
        Assert.Empty(capabilities.SpecTypes);
    }
}
