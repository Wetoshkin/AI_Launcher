using System.Collections.Generic;
using System.Linq;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.ViewModels;

public static class RuntimeReleaseSelectionController
{
    public static IReadOnlyList<RuntimeReleaseProfileOptionViewModel> ProfileOptions { get; } =
    [
        new(RuntimeReleaseProfile.Cpu),
        new(RuntimeReleaseProfile.Cuda),
        new(RuntimeReleaseProfile.Vulkan),
        new(RuntimeReleaseProfile.Rocm)
    ];

    public static IReadOnlyList<RuntimeReleaseSourceOptionViewModel> SourceOptions { get; } =
    [
        new(RuntimeReleaseAssetSource.Stable),
        new(RuntimeReleaseAssetSource.Latest),
        new(RuntimeReleaseAssetSource.Manual),
        new(RuntimeReleaseAssetSource.Detected)
    ];

    public static string ProfileHint(RuntimeReleaseProfile profile) => profile switch
    {
        RuntimeReleaseProfile.Cuda => "CUDA: для видеокарт NVIDIA, обычно самый быстрый вариант для RTX.",
        RuntimeReleaseProfile.Vulkan => "Vulkan: универсальный вариант для видеокарт NVIDIA, AMD и Intel.",
        RuntimeReleaseProfile.Rocm => "ROCm: для совместимых видеокарт AMD Radeon и Instinct.",
        _ => "Процессор: запуск без ускорения видеокартой, самый совместимый вариант."
    };

    public static IReadOnlyList<RuntimeReleasePackageRowViewModel> BuildPackageRows(
        IEnumerable<RuntimeReleasePackage> packages,
        RuntimeReleaseAssetSource source,
        int limit = 12) =>
        packages
            .Where(package => package.Source == source)
            .Take(limit)
            .Select(package => new RuntimeReleasePackageRowViewModel(package))
            .ToArray();
}
