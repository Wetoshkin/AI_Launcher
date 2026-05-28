using System.IO.Compression;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Tests;

public sealed class RuntimePackageInstallerTests
{
    [Fact]
    public async Task InstallAsyncExtractsArchiveUnderRuntimeRootAndReturnsLlamaServerPath()
    {
        using var temp = new TempDirectory();
        var archivePath = Path.Combine(temp.Path, "llama-runtime.zip");
        CreateZip(archivePath, new Dictionary<string, string>
        {
            ["llama/bin/llama-server.exe"] = "exe",
            ["llama/README.txt"] = "runtime"
        });
        var installer = new RuntimePackageInstaller();

        var result = await installer.InstallAsync(
            new RuntimePackageInstallRequest(archivePath, Path.Combine(temp.Path, "runtimes"), "llama-cpp-main"),
            CancellationToken.None);

        Assert.True(result.Installed);
        Assert.True(File.Exists(result.ExecutablePath));
        Assert.EndsWith(Path.Combine("llama-cpp-main", "llama", "bin", "llama-server.exe"), result.ExecutablePath);
        Assert.Contains("llama-server.exe найден", result.Message);
    }

    [Fact]
    public async Task InstallAsyncRejectsArchiveEntriesOutsideRuntimeDirectory()
    {
        using var temp = new TempDirectory();
        var archivePath = Path.Combine(temp.Path, "bad-runtime.zip");
        CreateZip(archivePath, new Dictionary<string, string>
        {
            ["../evil.exe"] = "bad"
        });
        var installer = new RuntimePackageInstaller();

        await Assert.ThrowsAsync<InvalidOperationException>(() => installer.InstallAsync(
            new RuntimePackageInstallRequest(archivePath, Path.Combine(temp.Path, "runtimes"), "bad"),
            CancellationToken.None));
    }

    [Fact]
    public async Task InstallAsyncReportsMissingLlamaServerExecutable()
    {
        using var temp = new TempDirectory();
        var archivePath = Path.Combine(temp.Path, "docs-only.zip");
        CreateZip(archivePath, new Dictionary<string, string>
        {
            ["README.txt"] = "runtime"
        });
        var installer = new RuntimePackageInstaller();

        var result = await installer.InstallAsync(
            new RuntimePackageInstallRequest(archivePath, Path.Combine(temp.Path, "runtimes"), "docs-only"),
            CancellationToken.None);

        Assert.False(result.Installed);
        Assert.Null(result.ExecutablePath);
        Assert.Contains("llama-server.exe не найден", result.Message);
    }

    private static void CreateZip(string archivePath, IReadOnlyDictionary<string, string> entries)
    {
        using var file = File.Create(archivePath);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "runtime-installer-" + Guid.NewGuid().ToString("N"));

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
