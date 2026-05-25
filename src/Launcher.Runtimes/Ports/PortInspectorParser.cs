using System.Text.Json;

namespace Launcher.Runtimes.Ports;

public static class PortInspectorParser
{
    public static PortOwnerInfo? ParsePowerShellJson(string json, bool endpointResponds, string? loadedModelId)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
        {
            if (root.GetArrayLength() == 0) return null;
            root = root[0];
        }

        var port = GetInt(root, "LocalPort");
        var pid = GetInt(root, "OwningProcess");
        if (port <= 0 || pid <= 0) return null;

        var processName = GetString(root, "ProcessName") ?? "";
        var path = GetString(root, "Path");

        return new PortOwnerInfo(port, pid, processName, path, endpointResponds, loadedModelId);
    }

    private static int GetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) return 0;
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var number) => number,
            _ => 0
        };
    }

    private static string? GetString(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
