namespace Launcher.Runtimes.LlamaCpp;

public sealed record RuntimeUpdateResult(
    bool UpdateAvailable,
    string? CurrentTag,
    string? LatestTag,
    RuntimeReleasePackage? LatestPackage,
    string Message);

public static class RuntimeUpdateService
{
    public static RuntimeUpdateResult Check(
        string runtimeArchivePath,
        IReadOnlyList<RuntimeReleasePackage> packages)
    {
        if (string.IsNullOrWhiteSpace(runtimeArchivePath))
        {
            return new RuntimeUpdateResult(false, null, packages.FirstOrDefault()?.TagName, packages.FirstOrDefault(), "укажите или скачайте архив runtime");
        }

        var latest = packages.FirstOrDefault();
        if (latest is null)
        {
            return new RuntimeUpdateResult(false, ExtractTag(runtimeArchivePath), null, null, "runtime-релизы не найдены");
        }

        var currentTag = ExtractTag(runtimeArchivePath);
        if (string.IsNullOrWhiteSpace(currentTag))
        {
            return new RuntimeUpdateResult(false, null, latest.TagName, latest, "не удалось определить версию runtime");
        }

        if (string.Equals(currentTag, latest.TagName, StringComparison.OrdinalIgnoreCase))
        {
            return new RuntimeUpdateResult(false, currentTag, latest.TagName, latest, $"runtime актуален: {latest.TagName}");
        }

        return new RuntimeUpdateResult(true, currentTag, latest.TagName, latest, $"доступно обновление: {currentTag} -> {latest.TagName}");
    }

    private static string? ExtractTag(string runtimeArchivePath)
    {
        var parts = runtimeArchivePath
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .Reverse();
        foreach (var part in parts)
        {
            var tag = ExtractTagFromText(part);
            if (tag is not null)
            {
                return tag;
            }
        }

        return null;
    }

    private static string? ExtractTagFromText(string text)
    {
        var tokens = text.Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries);
        return tokens.FirstOrDefault(token =>
            token.Length >= 2
            && char.ToLowerInvariant(token[0]) == 'b'
            && token.Skip(1).All(char.IsDigit));
    }
}
