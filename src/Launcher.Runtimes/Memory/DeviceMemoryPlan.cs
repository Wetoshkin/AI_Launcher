namespace Launcher.Runtimes.Memory;

public enum MemoryDeviceKind
{
    Gpu,
    SystemRam
}

/// <summary>Одно устройство в раскладке памяти: сколько занято, сколько отдаём под модель, ёмкость.</summary>
public sealed record DeviceMemoryRow(
    string Name,
    MemoryDeviceKind Kind,
    double CapacityGb,
    double BaseUsedGb,
    double ModelGb)
{
    public double FreeGb => Math.Max(0.0, CapacityGb - BaseUsedGb - ModelGb);

    /// <summary>Заполнение устройства в долях [0..1] с учётом перегруза.</summary>
    public double FillFraction => CapacityGb <= 0
        ? 0.0
        : Math.Min(1.0, (BaseUsedGb + ModelGb) / CapacityGb);

    public bool IsOverflowing => BaseUsedGb + ModelGb > CapacityGb + 0.01;
}

/// <summary>
/// Полная раскладка: куда ложится модель (веса+KV+overhead) по видеокартам и системной RAM,
/// и сколько не поместилось вообще.
/// </summary>
public sealed record DeviceMemoryPlan(
    IReadOnlyList<DeviceMemoryRow> Devices,
    double TotalModelGb,
    double OverflowGb)
{
    public bool Fits => OverflowGb <= 0.01 && Devices.All(d => !d.IsOverflowing);
}
