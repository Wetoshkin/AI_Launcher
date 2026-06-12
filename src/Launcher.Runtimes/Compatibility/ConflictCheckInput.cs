using Launcher.Core.Scenarios;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Memory;

namespace Launcher.Runtimes.Compatibility;

public enum RuntimeBackend
{
    Unknown,
    Cpu,
    Vulkan,
    Cuda,
    Rocm
}

/// <summary>Факты о выбранной модели, нужные для проверки совместимости настроек.</summary>
public sealed record ModelFacts(
    string Name,
    bool HasMtpHead,
    int? NativeContextTokens);

/// <summary>
/// Полный вход для <see cref="SettingsConflictEngine"/>: runtime, его возможности и бэкенд,
/// модель, и текущие настройки запуска (контекст, KV, MTP, speculative, раскладка памяти).
/// </summary>
public sealed record ConflictCheckInput(
    RuntimeKind RuntimeKind,
    LlamaServerCapabilities? Capabilities,
    RuntimeBackend Backend,
    bool HasNvidiaGpu,
    ModelFacts? Model,
    int ContextTokens,
    KvCacheProfile KvCache,
    bool MtpEnabled,
    bool SpeculativeEnabled,
    DeviceMemoryPlan? MemoryPlan);
