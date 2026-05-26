namespace Launcher.Runtimes.Memory;

public sealed record MemoryEstimate(
    double WeightsGb,
    double KvCacheGb,
    double KvBitsPerValue,
    double OverheadGb,
    double TotalGb);
