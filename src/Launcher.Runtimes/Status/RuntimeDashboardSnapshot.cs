using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Runtimes.Status;

public sealed record RuntimeDashboardSnapshot(
    double UsedGpuGb,
    double TotalGpuGb,
    bool IsPortFree,
    string GpuText,
    string PortText,
    string RuntimeText,
    LlamaRuntimeInfo? BestRuntime);
