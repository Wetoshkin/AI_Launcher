namespace Launcher.Core.Profiles;

public static class LaunchProfileModelIds
{
    public static string ProviderModelId(LaunchProfile profile)
    {
        var fileName = Path.GetFileNameWithoutExtension(profile.ModelPath);
        return string.IsNullOrWhiteSpace(fileName)
            ? "local/model"
            : $"local/{fileName}";
    }
}
