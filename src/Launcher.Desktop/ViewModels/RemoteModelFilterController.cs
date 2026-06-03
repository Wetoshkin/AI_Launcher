using System;
using System.Collections.Generic;
using System.Linq;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public static class RemoteModelFilterController
{
    public static IReadOnlyList<HuggingFaceModelSummary> ApplyModels(
        IEnumerable<HuggingFaceModelSummary> models,
        string familyFilter,
        string quantFilter,
        string sizeFilter,
        HuggingFaceCapabilityFilter? capabilityFilter)
    {
        var capabilityFiltered = capabilityFilter is null
            ? models
            : HuggingFaceCapabilityFilters.Apply(models, [capabilityFilter.Value]);

        return capabilityFiltered
            .Where(model => MatchesFamily(model, familyFilter))
            .Where(model => MatchesQuant(model, quantFilter))
            .Where(model => MatchesSize(model, sizeFilter))
            .ToArray();
    }

    public static bool MatchesSize(RemoteGgufDownloadOptionRowViewModel option, string sizeFilter) =>
        MatchesSize(option.Option, sizeFilter);

    public static bool MatchesSize(HuggingFaceGgufDownloadOption option, string sizeFilter)
    {
        if (IsAnySizeFilter(sizeFilter))
        {
            return true;
        }

        return FilterDownloadOptions([option], sizeFilter).Count == 1;
    }

    public static IReadOnlyList<HuggingFaceGgufDownloadOption> FilterDownloadOptions(
        IEnumerable<HuggingFaceGgufDownloadOption> options,
        string sizeFilter) =>
        HuggingFaceGgufDownloadSizeFilter.Apply(options, SizeRangeFor(sizeFilter));

    private static bool MatchesQuant(HuggingFaceModelSummary model, string quantFilter)
    {
        if (string.IsNullOrWhiteSpace(quantFilter)
            || quantFilter.Equals("любой", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return model.SiblingFiles?.Any(file => file.Contains(quantFilter, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool MatchesSize(HuggingFaceModelSummary model, string sizeFilter)
    {
        if (IsAnySizeFilter(sizeFilter))
        {
            return true;
        }

        return FilterDownloadOptions(HuggingFaceGgufFileSelector.SelectDownloadOptions(model), sizeFilter).Count > 0;
    }

    private static bool MatchesFamily(HuggingFaceModelSummary model, string familyFilter)
    {
        if (string.IsNullOrWhiteSpace(familyFilter)
            || familyFilter.Equals("любая", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return model.Id.Contains(familyFilter, StringComparison.OrdinalIgnoreCase)
            || model.Tags.Any(tag => tag.Contains(familyFilter, StringComparison.OrdinalIgnoreCase))
            || (model.SiblingFiles?.Any(file => file.Contains(familyFilter, StringComparison.OrdinalIgnoreCase)) == true);
    }

    private static bool IsAnySizeFilter(string sizeFilter) =>
        string.IsNullOrWhiteSpace(sizeFilter)
        || sizeFilter.Equals("любой размер", StringComparison.OrdinalIgnoreCase);

    private static HuggingFaceGgufDownloadSizeRange SizeRangeFor(string sizeFilter) => sizeFilter switch
    {
        "до 8 ГБ" => HuggingFaceGgufDownloadSizeRange.UpTo8Gb,
        "8-16 ГБ" => HuggingFaceGgufDownloadSizeRange.Between8And16Gb,
        "16-32 ГБ" => HuggingFaceGgufDownloadSizeRange.Between16And32Gb,
        "32+ ГБ" => HuggingFaceGgufDownloadSizeRange.Over32Gb,
        "неизвестный" => HuggingFaceGgufDownloadSizeRange.Unknown,
        _ => HuggingFaceGgufDownloadSizeRange.Any
    };
}
