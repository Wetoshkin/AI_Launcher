using System.Text.Json;
using Launcher.Core.Profiles;

namespace Launcher.Core.Migration;

public static class PythonLauncherConfigImporter
{
    public static LauncherSettings Import(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var modelsRoot = GetString(root, "models_dir") ?? "";
        var projectsRoot = GetString(root, "projects_dir");
        var serverPath = GetString(root, "llama_server_path") ?? "";
        var runtimeRoot = string.IsNullOrWhiteSpace(serverPath)
            ? ""
            : Path.GetDirectoryName(serverPath) ?? "";

        return new LauncherSettings(
            modelsRoot,
            projectsRoot,
            runtimeRoot,
            DownloadsRoot: modelsRoot,
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "on-demand",
            Profiles: Array.Empty<LaunchProfile>());
    }

    private static string? GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
