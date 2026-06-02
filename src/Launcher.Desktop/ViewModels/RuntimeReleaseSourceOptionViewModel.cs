using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.ViewModels;

public sealed class RuntimeReleaseSourceOptionViewModel(RuntimeReleaseAssetSource source)
{
    public RuntimeReleaseAssetSource Source => source;

    public string Label => RuntimeReleaseAssetSources.ToLabel(source);
}
