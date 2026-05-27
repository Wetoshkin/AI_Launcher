namespace Launcher.Runtimes.LlamaCpp;

public sealed record LlamaRuntimeInfo(
    string ExecutablePath,
    LlamaServerCapabilities Capabilities);
