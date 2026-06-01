namespace Launcher.Runtimes.LlamaCpp;

public sealed record RuntimeReleaseDownloadRequest(
    RuntimeReleasePackage Package,
    string CacheRoot);

public sealed record RuntimeReleaseDownloadResult(
    string ArchivePath,
    bool Downloaded,
    bool Skipped,
    string Message);

public interface IRuntimeReleaseDownloader
{
    Task<RuntimeReleaseDownloadResult> DownloadAsync(
        RuntimeReleaseDownloadRequest request,
        CancellationToken cancellationToken);
}

public sealed class RuntimeReleaseDownloadService(HttpClient httpClient) : IRuntimeReleaseDownloader
{
    private const int BufferSize = 81920;

    public async Task<RuntimeReleaseDownloadResult> DownloadAsync(
        RuntimeReleaseDownloadRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CacheRoot))
        {
            throw new ArgumentException("Runtime cache root is required.", nameof(request));
        }

        var targetPath = BuildSafeTargetPath(request.CacheRoot, request.Package);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        if (File.Exists(targetPath)
            && new FileInfo(targetPath).Length > 0
            && (request.Package.SizeBytes <= 0 || new FileInfo(targetPath).Length == request.Package.SizeBytes))
        {
            return new RuntimeReleaseDownloadResult(targetPath, Downloaded: false, Skipped: true, "архив уже скачан");
        }

        using var response = await httpClient.GetAsync(
            request.Package.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
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
                await CopyAsync(source, target, cancellationToken);
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
        return new RuntimeReleaseDownloadResult(targetPath, Downloaded: true, Skipped: false, "архив скачан");
    }

    private static async Task CopyAsync(Stream source, Stream target, CancellationToken cancellationToken)
    {
        var buffer = new byte[BufferSize];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static string BuildSafeTargetPath(string cacheRoot, RuntimeReleasePackage package)
    {
        var root = Path.GetFullPath(cacheRoot);
        var safeTag = SafeSegment(package.TagName, "runtime tag");
        var safeAsset = SafeSegment(package.AssetName, "runtime asset");
        var targetPath = Path.GetFullPath(Path.Combine(root, safeTag, safeAsset));
        var normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!targetPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Runtime archive path escapes cache root: {package.AssetName}");
        }

        return targetPath;
    }

    private static string SafeSegment(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value is "." or ".."
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || value.Contains('/')
            || value.Contains('\\'))
        {
            throw new InvalidOperationException($"Unsafe {label}: {value}");
        }

        return value;
    }
}
