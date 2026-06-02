namespace Launcher.Agents.Commands;

internal static class LocalOpenAiCommandRequestValidator
{
    public static void Validate(AgentLaunchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.ProviderModel.StartsWith("local/", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Local OpenAI-compatible agent commands require a provider model id in local/<gguf> format.",
                nameof(request));
        }
    }
}
