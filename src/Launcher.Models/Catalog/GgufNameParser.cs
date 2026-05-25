using System.Text.RegularExpressions;

namespace Launcher.Models.Catalog;

public static partial class GgufNameParser
{
    public static LocalModelFile Parse(string path)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        var family = fileName.StartsWith("Qwen", StringComparison.OrdinalIgnoreCase) ? "Qwen"
            : fileName.StartsWith("Gemma", StringComparison.OrdinalIgnoreCase) ? "Gemma"
            : fileName.StartsWith("DeepSeek", StringComparison.OrdinalIgnoreCase) ? "DeepSeek"
            : "Other";
        var size = SizeRegex().Match(fileName).Value;
        var quant = QuantRegex().Match(fileName).Value;
        var sizeGb = File.Exists(path) ? new FileInfo(path).Length / 1024d / 1024d / 1024d : 0;
        return new LocalModelFile(path, family, string.IsNullOrWhiteSpace(size) ? null : size, string.IsNullOrWhiteSpace(quant) ? null : quant, sizeGb);
    }

    [GeneratedRegex(@"\b\d+(?:\.\d+)?B\b", RegexOptions.IgnoreCase)]
    private static partial Regex SizeRegex();

    [GeneratedRegex(@"\b(?:Q[2-8](?:_[A-Z0-9]+)*|IQ[0-9A-Z_]+|F16|BF16)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuantRegex();
}
