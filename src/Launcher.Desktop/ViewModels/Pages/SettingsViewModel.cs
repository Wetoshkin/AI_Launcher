using System.Collections.Generic;
using Launcher.Core.Scenarios;
using Launcher.Runtimes.Compatibility;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Memory;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed class SettingsViewModel : ViewModelBase
{
    public string Title => "Настройки";
    public string Description => "Прокси, профили, режим Новичок/Эксперт. Здесь же — пример проверки конфликтов настроек.";

    /// <summary>Пример работы движка конфликтов (заведомо проблемная конфигурация).</summary>
    public IReadOnlyList<ConflictFinding> SampleFindings { get; }

    public SettingsViewModel()
    {
        var caps = new LlamaServerCapabilities(
            new HashSet<string>(), new HashSet<string>(), new HashSet<string>(),
            SupportsTurboQuant: false, SupportsMtp: true);

        var input = new ConflictCheckInput(
            RuntimeKind: RuntimeKind.LlamaCpp,
            Capabilities: caps,
            Backend: RuntimeBackend.Vulkan,
            HasNvidiaGpu: false,
            Model: new ModelFacts("Qwen2.5 7B", HasMtpHead: false, NativeContextTokens: 32768),
            ContextTokens: 65536,
            KvCache: KvCacheProfile.Symmetric("q8_0"),
            MtpEnabled: true,
            SpeculativeEnabled: false,
            MemoryPlan: null);

        SampleFindings = SettingsConflictEngine.Check(input);
    }
}
