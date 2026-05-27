namespace Launcher.Runtimes.LlamaCpp;

public interface ILlamaRuntimeCatalog
{
    Task<IReadOnlyList<LlamaRuntimeInfo>> ScanAsync(
        IEnumerable<string> runtimeRoots,
        CancellationToken cancellationToken);
}
