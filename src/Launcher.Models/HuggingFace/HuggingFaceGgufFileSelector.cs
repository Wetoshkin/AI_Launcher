using System.Text.RegularExpressions;

namespace Launcher.Models.HuggingFace;

public static partial class HuggingFaceGgufFileSelector
{
    public static IReadOnlyList<HuggingFaceGgufDownloadOption> SelectDownloadOptions(HuggingFaceModelSummary model)
    {
        var groups = new Dictionary<string, FileGroup>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileName in model.SiblingFiles ?? [])
        {
            if (!IsModelGguf(fileName))
            {
                continue;
            }

            var split = SplitShardRegex().Match(fileName);
            var groupFileName = split.Success
                ? SplitShardRegex().Replace(fileName, ".gguf")
                : fileName;
            var isFirstShard = !split.Success || split.Groups["index"].Value == "00001";

            if (!groups.TryGetValue(groupFileName, out var group))
            {
                group = new FileGroup(groupFileName, split.Success);
                groups.Add(groupFileName, group);
            }

            group.Files.Add(new HuggingFaceGgufFile(
                fileName,
                BuildResolveUrl(model.Id, fileName),
                isFirstShard));
        }

        return groups.Values
            .Select(group => new HuggingFaceGgufDownloadOption(
                Path.GetFileName(group.GroupFileName),
                ExtractQuant(group.GroupFileName),
                group.IsSplit,
                group.Files
                    .OrderBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .ToArray();
    }

    private static bool IsModelGguf(string fileName) =>
        fileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
        && !Path.GetFileName(fileName).Contains("mmproj", StringComparison.OrdinalIgnoreCase);

    private static string BuildResolveUrl(string repoId, string fileName)
    {
        var escapedPath = string.Join("/", fileName
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString));

        return $"https://huggingface.co/{repoId}/resolve/main/{escapedPath}";
    }

    private static string? ExtractQuant(string fileName)
    {
        var match = QuantRegex().Match(fileName);
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex(@"-(?<index>\d{5})-of-(?<total>\d{5})\.gguf$", RegexOptions.IgnoreCase)]
    private static partial Regex SplitShardRegex();

    [GeneratedRegex(@"\b(?:Q[2-8](?:_[A-Z0-9]+)*|IQ[0-9A-Z_]+|F16|BF16|F32|FP32)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuantRegex();

    private sealed class FileGroup(string groupFileName, bool isSplit)
    {
        public string GroupFileName { get; } = groupFileName;

        public bool IsSplit { get; } = isSplit;

        public List<HuggingFaceGgufFile> Files { get; } = [];
    }
}
