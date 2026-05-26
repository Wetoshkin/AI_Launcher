using Launcher.Core.Decoding;
using Launcher.Core.LaunchPlans;
using Launcher.Core.Profiles;

namespace Launcher.Runtimes.LlamaCpp;

public static class LlamaServerCommandBuilder
{
    public static LaunchPlan Build(LaunchProfile profile, DecodingPreset decodingPreset)
    {
        var arguments = new List<string>
        {
            "-m",
            profile.ModelPath,
            "--ctx-size",
            profile.ContextTokens.ToString(),
            "--port",
            profile.Port.ToString()
        };

        foreach (var argument in decodingPreset.Arguments)
        {
            arguments.Add(argument.Key);
            arguments.Add(argument.Value);
        }

        return new LaunchPlan(
            "llama-server",
            arguments,
            new Dictionary<string, string>());
    }
}
