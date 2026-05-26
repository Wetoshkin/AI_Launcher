using System.Text.RegularExpressions;

namespace Launcher.Models.Catalog;

public static partial class LocalModelCatalog
{
    private const long MinimumModelSizeBytes = 128L * 1024L * 1024L;

    public static IReadOnlyList<LocalModelFile> Scan(IEnumerable<string> modelDirectories)
    {
        var models = new Dictionary<string, LocalModelFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in modelDirectories.Where(Directory.Exists))
        {
            foreach (var path in Directory.EnumerateFiles(directory, "*.gguf", SearchOption.AllDirectories))
            {
                if (!ShouldInclude(path))
                {
                    continue;
                }

                var fullPath = Path.GetFullPath(path);
                models[fullPath] = GgufNameParser.Parse(fullPath);
            }
        }

        return models.Values
            .OrderBy(model => model.Family)
            .ThenByDescending(model => model.SizeGb)
            .ThenBy(model => Path.GetFileName(model.Path), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ShouldInclude(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.Contains("mmproj", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var info = new FileInfo(path);
        if (!info.Exists || info.Length < MinimumModelSizeBytes)
        {
            return false;
        }

        var splitMatch = SplitShardRegex().Match(fileName);
        return !splitMatch.Success || splitMatch.Groups[1].Value == "00001";
    }

    [GeneratedRegex(@"-(\d{5})-of-(\d{5})\.gguf$", RegexOptions.IgnoreCase)]
    private static partial Regex SplitShardRegex();
}
