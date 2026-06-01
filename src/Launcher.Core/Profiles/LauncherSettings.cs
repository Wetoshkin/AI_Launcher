namespace Launcher.Core.Profiles;

public sealed record LauncherSettings(
    string ModelsRoot,
    string? ProjectsRoot,
    string RuntimeRoot,
    string DownloadsRoot,
    int DefaultPort,
    string Language,
    string HelpMode,
    IReadOnlyList<LaunchProfile> Profiles)
{
    public string? LastRuntimeVersionSource { get; init; }
}
