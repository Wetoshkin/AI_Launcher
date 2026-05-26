using System.Text.RegularExpressions;

namespace Launcher.Runtimes.LlamaCpp;

public static partial class LlamaServerHelpParser
{
    public static LlamaServerCapabilities Parse(string helpText)
    {
        var flags = FlagRegex()
            .Matches(helpText)
            .Select(match => match.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cacheTypes = CacheTypeRegex()
            .Matches(helpText)
            .Select(match => match.Value.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var specTypes = SpecTypeRegex()
            .Matches(helpText)
            .Select(match => match.Value.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var supportsTurboQuant = cacheTypes.Contains("turbo3")
            || cacheTypes.Contains("turbo4")
            || cacheTypes.Contains("tbq3_0")
            || cacheTypes.Contains("tbq4_0");

        var supportsMtp = specTypes.Contains("draft-mtp");

        return new LlamaServerCapabilities(flags, cacheTypes, specTypes, supportsTurboQuant, supportsMtp);
    }

    [GeneratedRegex(@"(?<!\S)(?:--[a-zA-Z0-9][a-zA-Z0-9-]*|-[a-zA-Z][a-zA-Z0-9]*)")]
    private static partial Regex FlagRegex();

    [GeneratedRegex(@"\b(?:f16|bf16|q8_0|q4_0|q4_1|iq4_nl|turbo3|turbo4|tbq3_0|tbq4_0)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CacheTypeRegex();

    [GeneratedRegex(@"\b(?:draft-mtp|draft|none)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SpecTypeRegex();
}
