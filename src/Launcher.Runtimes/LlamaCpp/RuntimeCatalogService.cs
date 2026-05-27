using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.LlamaCpp;

public sealed class RuntimeCatalogService(ICommandRunner commandRunner) : ILlamaRuntimeCatalog
{
    public async Task<IReadOnlyList<LlamaRuntimeInfo>> ScanAsync(
        IEnumerable<string> runtimeRoots,
        CancellationToken cancellationToken)
    {
        var runtimes = new List<LlamaRuntimeInfo>();
        foreach (var root in runtimeRoots.Where(Directory.Exists))
        {
            foreach (var executable in Directory.EnumerateFiles(root, "llama-server.exe", SearchOption.AllDirectories))
            {
                var help = await commandRunner.RunAsync(executable, "--help", cancellationToken);
                runtimes.Add(new LlamaRuntimeInfo(executable, LlamaServerHelpParser.Parse(help)));
            }
        }

        return runtimes
            .OrderByDescending(runtime => runtime.Capabilities.SupportsMtp)
            .ThenByDescending(runtime => runtime.Capabilities.SupportsTurboQuant)
            .ThenBy(runtime => runtime.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
