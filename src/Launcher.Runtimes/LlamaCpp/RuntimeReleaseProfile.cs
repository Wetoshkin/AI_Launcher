namespace Launcher.Runtimes.LlamaCpp;

public enum RuntimeReleaseProfile
{
    Cpu,
    Cuda,
    Vulkan,
    Rocm
}

public static class RuntimeReleaseProfileFragments
{
    public static IReadOnlyList<string> For(RuntimeReleaseProfile profile) => profile switch
    {
        RuntimeReleaseProfile.Cuda => ["win", "cuda", "x64"],
        RuntimeReleaseProfile.Vulkan => ["win", "vulkan", "x64"],
        RuntimeReleaseProfile.Rocm => ["win", "hip", "x64"],
        _ => ["bin-win-x64.zip"]
    };
}
