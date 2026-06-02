namespace Launcher.Models.HuggingFace;

public static class HuggingFaceCapabilityFilters
{
    public static IReadOnlyList<HuggingFaceModelSummary> Apply(
        IEnumerable<HuggingFaceModelSummary> models,
        IReadOnlyCollection<HuggingFaceCapabilityFilter> filters)
    {
        if (filters.Count == 0)
        {
            return models.ToArray();
        }

        return models
            .Where(model => filters.All(filter => Matches(model, filter)))
            .ToArray();
    }

    public static IReadOnlyList<HuggingFaceCapabilityFilter> GetCapabilities(HuggingFaceModelSummary model)
    {
        return Enum.GetValues<HuggingFaceCapabilityFilter>()
            .Where(filter => Matches(model, filter))
            .ToArray();
    }

    public static bool Matches(HuggingFaceModelSummary model, HuggingFaceCapabilityFilter filter)
    {
        return filter switch
        {
            HuggingFaceCapabilityFilter.Gguf => HasGguf(model),
            HuggingFaceCapabilityFilter.Vision => HasVision(model),
            HuggingFaceCapabilityFilter.Tools => HasTools(model),
            HuggingFaceCapabilityFilter.Mtp => HasMtp(model),
            HuggingFaceCapabilityFilter.RuntimeCompatible => model.IsRuntimeCompatible,
            HuggingFaceCapabilityFilter.TurboQuantCompatible =>
                HasGguf(model) && model.HasPreferredQuant && model.IsRuntimeCompatible,
            _ => false
        };
    }

    private static bool HasGguf(HuggingFaceModelSummary model)
    {
        return HasTag(model, "gguf")
            || model.Id.Contains("GGUF", StringComparison.OrdinalIgnoreCase)
            || GetSiblingFileNames(model).Any(IsModelGguf);
    }

    private static bool HasVision(HuggingFaceModelSummary model)
    {
        return HasAnyTag(model, "vision", "image-to-text", "image-text-to-text", "multimodal", "visual-language", "vision-language", "vlm")
            || GetSiblingFileNames(model).Any(IsMmprojFile);
    }

    private static bool HasTools(HuggingFaceModelSummary model)
    {
        return HasAnyNormalizedTag(model, "tooluse", "toolcalling", "functioncalling", "functioncall");
    }

    private static bool HasMtp(HuggingFaceModelSummary model)
    {
        return HasAnyNormalizedTag(model, "mtp", "multitokenprediction")
            || GetSiblingFileNames(model).Any(file => Path.GetFileName(file).Contains("MTP", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasTag(HuggingFaceModelSummary model, string expected)
    {
        return model.Tags.Any(tag => string.Equals(tag, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAnyTag(HuggingFaceModelSummary model, params string[] expectedTags)
    {
        return model.Tags.Any(tag => expectedTags.Any(expected => string.Equals(tag, expected, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasAnyNormalizedTag(HuggingFaceModelSummary model, params string[] expectedTags)
    {
        return model.Tags
            .Select(Normalize)
            .Any(tag => expectedTags.Any(expected => string.Equals(tag, expected, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> GetSiblingFileNames(HuggingFaceModelSummary model)
    {
        if (model.SiblingFileMetadata is { Count: > 0 })
        {
            return model.SiblingFileMetadata
                .Select(file => file.FileName)
                .Where(fileName => !string.IsNullOrWhiteSpace(fileName));
        }

        return model.SiblingFiles ?? [];
    }

    private static bool IsModelGguf(string fileName)
    {
        return fileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
            && !IsMmprojFile(fileName);
    }

    private static bool IsMmprojFile(string fileName)
    {
        return Path.GetFileName(fileName).Contains("mmproj", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal);
    }
}
