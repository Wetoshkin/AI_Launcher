namespace Launcher.Core.Profiles;

public interface ILauncherSettingsStore
{
    Task<LauncherSettings?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken);
}
