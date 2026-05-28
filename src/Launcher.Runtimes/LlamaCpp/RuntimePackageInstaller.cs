using System.IO.Compression;

namespace Launcher.Runtimes.LlamaCpp;

public sealed class RuntimePackageInstaller : IRuntimePackageInstaller
{
    public async Task<RuntimePackageInstallResult> InstallAsync(
        RuntimePackageInstallRequest request,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(request.ArchivePath))
        {
            throw new FileNotFoundException("Runtime archive was not found.", request.ArchivePath);
        }

        var installDirectory = BuildInstallDirectory(request.RuntimeRoot, request.RuntimeId);
        Directory.CreateDirectory(installDirectory);

        using var archive = ZipFile.OpenRead(request.ArchivePath);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var targetPath = BuildSafeEntryPath(installDirectory, entry.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await using var source = entry.Open();
            await using var target = File.Create(targetPath);
            await source.CopyToAsync(target, cancellationToken);
        }

        var executable = Directory
            .EnumerateFiles(installDirectory, "llama-server.exe", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (executable is null)
        {
            return new RuntimePackageInstallResult(
                Installed: false,
                installDirectory,
                ExecutablePath: null,
                "llama-server.exe не найден в архиве runtime.");
        }

        return new RuntimePackageInstallResult(
            Installed: true,
            installDirectory,
            executable,
            $"llama-server.exe найден: {executable}");
    }

    private static string BuildInstallDirectory(string runtimeRoot, string runtimeId)
    {
        var root = Path.GetFullPath(runtimeRoot);
        var safeId = string.Join("-", runtimeId
            .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0));
        if (string.IsNullOrWhiteSpace(safeId))
        {
            throw new InvalidOperationException("Runtime id is empty after sanitization.");
        }

        var installDirectory = Path.GetFullPath(Path.Combine(root, safeId));
        EnsureInsideRoot(root, installDirectory, runtimeId);
        return installDirectory;
    }

    private static string BuildSafeEntryPath(string installDirectory, string entryName)
    {
        var targetPath = Path.GetFullPath(Path.Combine(installDirectory, entryName));
        EnsureInsideRoot(installDirectory, targetPath, entryName);
        return targetPath;
    }

    private static void EnsureInsideRoot(string root, string path, string source)
    {
        var normalizedRoot = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Runtime package path escapes the install directory: {source}");
        }
    }
}
