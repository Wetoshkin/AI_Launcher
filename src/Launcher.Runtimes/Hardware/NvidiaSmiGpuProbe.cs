using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Hardware;

public sealed class NvidiaSmiGpuProbe(ICommandRunner commandRunner) : IGpuProbe
{
    private const string QueryArguments =
        "--query-gpu=name,memory.used,memory.total --format=csv,noheader,nounits";

    public async Task<IReadOnlyList<GpuInfo>> GetGpusAsync(CancellationToken cancellationToken)
    {
        var output = await commandRunner.RunAsync("nvidia-smi", QueryArguments, cancellationToken);
        return NvidiaSmiParser.ParseGpuRows(output);
    }
}
