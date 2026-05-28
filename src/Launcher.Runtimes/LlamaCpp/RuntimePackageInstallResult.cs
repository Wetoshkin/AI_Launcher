namespace Launcher.Runtimes.LlamaCpp;

public sealed record RuntimePackageInstallResult(
    bool Installed,
    string InstallDirectory,
    string? ExecutablePath,
    string Message);
