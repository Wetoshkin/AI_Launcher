namespace Launcher.Models.HuggingFace;

public sealed class HuggingFaceModelDownloadService(HttpClient httpClient) : IHuggingFaceModelDownloadService
{
    public async Task<HuggingFaceModelDownloadResult> DownloadAsync(
        HuggingFaceModelDownloadRequest request,
        CancellationToken cancellationToken,
        Action<HuggingFaceDownloadProgress>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(request.ModelsDirectory))
        {
            throw new ArgumentException("Models directory is required.", nameof(request));
        }

        var repoRoot = BuildRepoRoot(request.ModelsDirectory, request.RepoId);
        Directory.CreateDirectory(repoRoot);

        var downloaded = new List<string>();
        var skipped = new List<string>();
        var totalFiles = request.Option.Files.Count;

        for (var index = 0; index < request.Option.Files.Count; index++)
        {
            var file = request.Option.Files[index];
            var targetPath = BuildSafeTargetPath(repoRoot, file.FileName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 0)
            {
                skipped.Add(targetPath);
                progress?.Invoke(new HuggingFaceDownloadProgress(
                    file.FileName,
                    index + 1,
                    totalFiles,
                    new FileInfo(targetPath).Length,
                    new FileInfo(targetPath).Length,
                    IsSkipped: true));
                continue;
            }

            using var response = await httpClient.GetAsync(file.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var tempPath = targetPath + ".download";
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var target = File.Create(tempPath))
            {
                await source.CopyToAsync(target, cancellationToken);
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);
            downloaded.Add(targetPath);

            var length = new FileInfo(targetPath).Length;
            progress?.Invoke(new HuggingFaceDownloadProgress(
                file.FileName,
                index + 1,
                totalFiles,
                length,
                response.Content.Headers.ContentLength,
                IsSkipped: false));
        }

        return new HuggingFaceModelDownloadResult(downloaded, skipped);
    }

    private static string BuildRepoRoot(string modelsDirectory, string repoId)
    {
        var repoSegments = SplitSafeRelativePath(repoId);
        return Path.GetFullPath(Path.Combine([Path.GetFullPath(modelsDirectory), .. repoSegments]));
    }

    private static string BuildSafeTargetPath(string repoRoot, string fileName)
    {
        if (Path.IsPathRooted(fileName))
        {
            throw new InvalidOperationException($"Hugging Face file path must be relative: {fileName}");
        }

        var fileSegments = SplitSafeRelativePath(fileName);
        var targetPath = Path.GetFullPath(Path.Combine([repoRoot, .. fileSegments]));
        var normalizedRoot = repoRoot.EndsWith(Path.DirectorySeparatorChar)
            ? repoRoot
            : repoRoot + Path.DirectorySeparatorChar;

        if (!targetPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Hugging Face file path escapes the models directory: {fileName}");
        }

        return targetPath;
    }

    private static string[] SplitSafeRelativePath(string path)
    {
        var segments = path
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Trim())
            .ToArray();

        if (segments.Length == 0
            || segments.Any(segment => segment is "." or ".." || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            throw new InvalidOperationException($"Unsafe Hugging Face path: {path}");
        }

        return segments;
    }
}
