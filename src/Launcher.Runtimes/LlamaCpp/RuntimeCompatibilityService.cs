using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Runtimes.LlamaCpp;

public static class RuntimeCompatibilityService
{
    public static RuntimeCompatibilityResult Check(LaunchProfile profile, LlamaRuntimeInfo? runtime)
    {
        if (profile.Runtime is RuntimeKind.Ollama)
        {
            return new RuntimeCompatibilityResult(true, ["Ollama runtime проверяется отдельно."]);
        }

        if (runtime is null)
        {
            return new RuntimeCompatibilityResult(false, ["Runtime llama-server не проверен."]);
        }

        var messages = new List<string>();
        var compatible = true;

        if (profile.Runtime is RuntimeKind.LlamaCppMtp)
        {
            if (runtime.Capabilities.SupportsMtp)
            {
                messages.Add("MTP поддерживается");
            }
            else
            {
                messages.Add("Выбран MTP, но runtime не поддерживает --spec-type draft-mtp.");
                compatible = false;
            }
        }

        if (profile.Runtime is RuntimeKind.LlamaCppTurboQuant)
        {
            if (runtime.Capabilities.SupportsTurboQuant)
            {
                messages.Add("TurboQuant поддерживается");
            }
            else
            {
                messages.Add("Выбран TurboQuant, но runtime не поддерживает TurboQuant-флаги.");
                compatible = false;
            }
        }

        if (messages.Count == 0)
        {
            messages.Add("Runtime совместим с выбранным режимом.");
        }

        return new RuntimeCompatibilityResult(compatible, messages);
    }
}
