namespace Launcher.Core.Profiles;

public sealed class LauncherSettingsFileStore(string filePath) : ILauncherSettingsStore
{
    public async Task<LauncherSettings?> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return ProfileSerializer.DeserializeSettings(json);
    }

    public async Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = ProfileSerializer.SerializeSettings(settings);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }
}
