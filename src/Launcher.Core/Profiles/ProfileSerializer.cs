using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launcher.Core.Profiles;

public static class ProfileSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize(LaunchProfile profile)
    {
        return JsonSerializer.Serialize(profile, Options);
    }

    public static LaunchProfile DeserializeProfile(string json)
    {
        return JsonSerializer.Deserialize<LaunchProfile>(json, Options)
            ?? throw new InvalidOperationException("Profile JSON is empty.");
    }
}
