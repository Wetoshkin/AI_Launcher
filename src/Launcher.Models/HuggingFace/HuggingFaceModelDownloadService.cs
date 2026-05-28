namespace Launcher.Models.HuggingFace;

public sealed class HuggingFaceModelDownloadService(HttpClient httpClient) : IHuggingFaceModelDownloadService
{
    private const int BufferSize = 81920;

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

            try
            {
                await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var target = File.Create(tempPath))
                {
                    await CopyWithProgressAsync(
                        source,
                        target,
                        file.FileName,
                        index + 1,
                        totalFiles,
                        response.Content.Headers.ContentLength,
                        progress,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempPath, targetPath);
            downloaded.Add(targetPath);

            var length = new FileInfo(targetPath).Length;
            ReportProgress(file.FileName, index + 1, totalFiles, length, response.Content.Headers.ContentLength, progress, IsSkipped: false);
        }

        return new HuggingFaceModelDownloadResult(downloaded, skipped);
    }

    private static async Task CopyWithProgressAsync(
        Stream source,
        Stream target,
        string fileName,
        int fileIndex,
        int totalFiles,
        long? totalBytes,
        Action<HuggingFaceDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];
        long received = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            received += read;
            ReportProgress(fileName, fileIndex, totalFiles, received, totalBytes, progress, IsSkipped: false);
        }
    }

    private static void ReportProgress(
        string fileName,
        int fileIndex,
        int totalFiles,
        long bytesReceived,
        long? totalBytes,
        Action<HuggingFaceDownloadProgress>? progress,
        bool IsSkipped)
    {
        progress?.Invoke(new HuggingFaceDownloadProgress(
            fileName,
            fileIndex,
            totalFiles,
            bytesReceived,
            totalBytes,
            IsSkipped));
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
