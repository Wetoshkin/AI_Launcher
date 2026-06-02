using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.ViewModels;

public sealed class RuntimeReleaseProfileOptionViewModel(RuntimeReleaseProfile profile)
{
    public RuntimeReleaseProfile Profile => profile;

    public string Label => profile switch
    {
        RuntimeReleaseProfile.Cuda => "NVIDIA CUDA",
        RuntimeReleaseProfile.Vulkan => "Vulkan для видеокарт",
        RuntimeReleaseProfile.Rocm => "AMD ROCm",
        _ => "Процессор"
    };

    public string Tooltip => profile switch
    {
        RuntimeReleaseProfile.Cuda => "Для видеокарт NVIDIA, обычно лучший выбор для RTX.",
        RuntimeReleaseProfile.Vulkan => "Универсальный вариант для видеокарт NVIDIA, AMD и Intel.",
        RuntimeReleaseProfile.Rocm => "Для совместимых видеокарт AMD Radeon и Instinct.",
        _ => "Самый совместимый вариант без ускорения видеокартой."
    };
}
