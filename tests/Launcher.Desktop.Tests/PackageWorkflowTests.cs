using System.Text.RegularExpressions;

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
}
