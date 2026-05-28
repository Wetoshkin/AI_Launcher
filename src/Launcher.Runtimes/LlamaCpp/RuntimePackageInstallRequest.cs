namespace Launcher.Runtimes.LlamaCpp;

public sealed record RuntimePackageInstallRequest(
    string ArchivePath,
    string RuntimeRoot,
    string RuntimeId);
