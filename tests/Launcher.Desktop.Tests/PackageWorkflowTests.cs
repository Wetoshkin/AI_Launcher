using System.Text.RegularExpressions;
using System.IO.Compression;

namespace Launcher.Desktop.Tests;

public sealed class PackageWorkflowTests
{
    [Fact]
    public void PackageWorkflowKeepsPortableArtifactNamesConsistent()
    {
        var workflow = ReadPackageWorkflow();

        Assert.Contains(
            "PACKAGE_BASENAME=AI-Launcher-Studio-$packageVersion-win-x64",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(
            "package_basename=AI-Launcher-Studio-$packageVersion-win-x64",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(
            "name: AI-Launcher-Studio-${{ steps.meta.outputs.package_version }}-win-x64",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(
            @".\publish\${{ steps.meta.outputs.package_basename }}.zip",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(
            @".\publish\${{ steps.meta.outputs.package_basename }}.zip.sha256",
            workflow,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PackageWorkflowVerifiesPortableZipSha256()
    {
        var workflow = ReadPackageWorkflow();
        var verifyStep = ExtractStep(workflow, "Verify portable package SHA256");

        Assert.Contains(@"$zipPath = "".\publish\$env:PACKAGE_BASENAME.zip""", verifyStep, StringComparison.Ordinal);
        Assert.Contains(@"$hashPath = ""$zipPath.sha256""", verifyStep, StringComparison.Ordinal);
        Assert.Contains("Get-FileHash -Path $zipPath -Algorithm SHA256", verifyStep, StringComparison.Ordinal);
        Assert.Contains("SHA256 mismatch", verifyStep, StringComparison.Ordinal);
    }

    [Fact]
    public void PackageWorkflowUploadsReleasePrepGuideAndReleaseNotes()
    {
        var workflow = ReadPackageWorkflow();

        Assert.Contains(@"$guidePath = "".\publish\GITHUB_RELEASE_PREP_RU.md""", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "name: AI-Launcher-Studio-${{ steps.meta.outputs.package_version }}-release-prep",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(@".\publish\GITHUB_RELEASE_PREP_RU.md", workflow, StringComparison.Ordinal);
        Assert.Contains(
            "name: AI-Launcher-Studio-${{ steps.meta.outputs.package_version }}-release-notes",
            workflow,
            StringComparison.Ordinal);
        Assert.Contains(@".\docs\RELEASE_NOTES_RU.md", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void PackageWorkflowCanPublishGitHubReleaseForTagsOrManualInput()
    {
        var workflow = ReadPackageWorkflow();
        var metadataStep = ExtractStep(workflow, "Prepare package metadata");
        var releaseStep = ExtractStep(workflow, "Publish GitHub Release");

        Assert.Contains("contents: write", workflow, StringComparison.Ordinal);
        Assert.Contains("publish_release:", workflow, StringComparison.Ordinal);
        Assert.Contains("release_tag:", workflow, StringComparison.Ordinal);
        Assert.Contains("SHOULD_PUBLISH_RELEASE", metadataStep, StringComparison.Ordinal);
        Assert.Contains("release_tag is required", metadataStep, StringComparison.Ordinal);
        Assert.Contains("if: steps.meta.outputs.should_publish_release == 'true'", releaseStep, StringComparison.Ordinal);
        Assert.Contains("gh release create", releaseStep, StringComparison.Ordinal);
        Assert.Contains("gh release upload", releaseStep, StringComparison.Ordinal);
        Assert.Contains("--clobber", releaseStep, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallPortablePackageRejectsZipEntriesEscapingDestination()
    {
        using var temp = new TempDirectory();
        var zipPath = Path.Combine(temp.Path, "evil.zip");
        var installPath = Path.Combine(temp.Path, "install");
        var escapedPath = Path.Combine(temp.Path, "evil.txt");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("../evil.txt");
            await using var stream = entry.Open();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync("escaped");
        }

        var result = await RunPowerShellAsync(
            RepositoryFile("scripts", "Install-PortablePackage.ps1"),
            "-ZipPath",
            zipPath,
            "-Destination",
            installPath);

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("Unsafe zip entry escapes destination", result.CombinedOutput);
        Assert.False(File.Exists(escapedPath));
    }

    [Fact]
    public async Task InstallPortablePackageCanCreateStartMenuShortcutInCustomDirectory()
    {
        using var temp = new TempDirectory();
        var zipPath = Path.Combine(temp.Path, "AI-Launcher-Studio-win-x64.zip");
        var installPath = Path.Combine(temp.Path, "install");
        var shortcutRoot = Path.Combine(temp.Path, "shortcuts");
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("Launcher.Desktop.exe");
            await using var stream = entry.Open();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync("fake exe for installer smoke");
        }

        var result = await RunPowerShellAsync(
            RepositoryFile("scripts", "Install-PortablePackage.ps1"),
            "-ZipPath",
            zipPath,
            "-Destination",
            installPath,
            "-CreateStartMenuShortcut",
            "-StartMenuDirectory",
            shortcutRoot,
            "-ShortcutName",
            "AI Launcher Studio Test");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Installed portable AI Launcher Studio", result.CombinedOutput);
        Assert.Contains("Start Menu shortcut", result.CombinedOutput);
        Assert.True(File.Exists(Path.Combine(installPath, "Launcher.Desktop.exe")));
        Assert.True(File.Exists(Path.Combine(shortcutRoot, "AI Launcher Studio", "AI Launcher Studio Test.lnk")));
    }

    private static string ReadPackageWorkflow()
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var workflowPath = Path.Combine(repositoryRoot, ".github", "workflows", "package.yml");

        Assert.True(File.Exists(workflowPath), $"Package workflow not found: {workflowPath}");
        return File.ReadAllText(workflowPath);
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var workflowPath = Path.Combine(directory.FullName, ".github", "workflows", "package.yml");
            if (File.Exists(workflowPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Could not find repository root from {startDirectory}.");
    }

    private static string ExtractStep(string workflow, string stepName)
    {
        var pattern = $@"(?ms)^\s*- name: {Regex.Escape(stepName)}\s*$.*?(?=^\s*- name: |\z)";
        var match = Regex.Match(workflow, pattern);

        Assert.True(match.Success, $"Workflow step not found: {stepName}");
        return match.Value;
    }

    private static string RepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        return Path.Combine(new[] { repositoryRoot }.Concat(pathParts).ToArray());
    }

    private static async Task<ProcessResult> RunPowerShellAsync(string scriptPath, params string[] arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "powershell",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(scriptPath);
        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Не удалось запустить PowerShell.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new ProcessResult(process.ExitCode, stdout + stderr);
    }

    private sealed record ProcessResult(int ExitCode, string CombinedOutput);

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "launcher-package-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
