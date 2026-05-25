namespace Launcher.Models.Catalog;

public static class ModelFilterService
{
    public static IReadOnlyList<LocalModelFile> Apply(IEnumerable<LocalModelFile> models, ModelFilter filter)
    {
        var query = models;
        if (!string.IsNullOrWhiteSpace(filter.Family))
            query = query.Where(m => string.Equals(m.Family, filter.Family, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Quant))
            query = query.Where(m => m.Quant?.Contains(filter.Quant, StringComparison.OrdinalIgnoreCase) == true);
        if (filter.MaxSizeGb is { } max)
            query = query.Where(m => m.SizeGb <= max);
        return query.ToList();
    }
}
