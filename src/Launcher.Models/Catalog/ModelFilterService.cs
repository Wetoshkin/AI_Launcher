namespace Launcher.Models.Catalog;

public static class ModelFilterService
{
    public static IReadOnlyList<LocalModelFile> Apply(IEnumerable<LocalModelFile> models, ModelFilter filter)
    {
        var query = models;
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var text = filter.Query.Trim();
            query = query.Where(m =>
                Path.GetFileName(m.Path).Contains(text, StringComparison.OrdinalIgnoreCase)
                || m.Path.Contains(text, StringComparison.OrdinalIgnoreCase)
                || m.Family.Contains(text, StringComparison.OrdinalIgnoreCase)
                || m.Quant?.Contains(text, StringComparison.OrdinalIgnoreCase) == true
                || m.SizeLabel?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(filter.Family))
            query = query.Where(m => string.Equals(m.Family, filter.Family, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Quant))
            query = query.Where(m => m.Quant?.Contains(filter.Quant, StringComparison.OrdinalIgnoreCase) == true);
        if (filter.MaxSizeGb is { } max)
            query = query.Where(m => m.SizeGb <= max);
        return query.ToList();
    }
}
