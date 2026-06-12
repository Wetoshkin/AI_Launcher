using System.Linq;
using Launcher.Core.Scenarios;
using Launcher.Runtimes.Compatibility;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Memory;

namespace Launcher.Runtimes.Tests.Compatibility;

public class SettingsConflictEngineTests
{
    private static LlamaServerCapabilities Caps(bool mtp = false, bool turbo = false) =>
        new(new HashSet<string>(), new HashSet<string>(), new HashSet<string>(), turbo, mtp);

    private static ConflictCheckInput BaseInput() => new(
        RuntimeKind: RuntimeKind.LlamaCpp,
        Capabilities: Caps(),
        Backend: RuntimeBackend.Vulkan,
        HasNvidiaGpu: false,
        Model: new ModelFacts("Qwen2.5 7B", HasMtpHead: false, NativeContextTokens: 32768),
        ContextTokens: 8192,
        KvCache: KvCacheProfile.Symmetric("q8_0"),
        MtpEnabled: false,
        SpeculativeEnabled: false,
        MemoryPlan: null);

    private static bool Has(IReadOnlyList<ConflictFinding> findings, ConflictSeverity sev, string fragment) =>
        findings.Any(f => f.Severity == sev && f.Title.Contains(fragment, System.StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Clean_setup_has_no_errors()
    {
        var findings = SettingsConflictEngine.Check(BaseInput());
        Assert.DoesNotContain(findings, f => f.Severity == ConflictSeverity.Error);
    }

    [Fact]
    public void Mtp_without_mtp_model_is_error()
    {
        var input = BaseInput() with { MtpEnabled = true, Capabilities = Caps(mtp: true) };
        var findings = SettingsConflictEngine.Check(input);
        Assert.True(Has(findings, ConflictSeverity.Error, "MTP"));
    }

    [Fact]
    public void Mtp_without_runtime_support_is_error()
    {
        var input = BaseInput() with
        {
            MtpEnabled = true,
            Model = new ModelFacts("Qwen3.6 MTP", true, 32768),
            Capabilities = Caps(mtp: false)
        };
        var findings = SettingsConflictEngine.Check(input);
        Assert.True(Has(findings, ConflictSeverity.Error, "runtime"));
    }

    [Fact]
    public void Turbo_kv_without_turboquant_runtime_is_error()
    {
        var input = BaseInput() with { KvCache = KvCacheProfile.Symmetric("turbo4"), Capabilities = Caps(turbo: false) };
        var findings = SettingsConflictEngine.Check(input);
        Assert.True(Has(findings, ConflictSeverity.Error, "TurboQuant"));
    }

    [Fact]
    public void Context_above_native_is_warning()
    {
        var input = BaseInput() with { ContextTokens = 65536 };
        var findings = SettingsConflictEngine.Check(input);
        Assert.True(Has(findings, ConflictSeverity.Warning, "контекст"));
    }

    [Fact]
    public void Cuda_build_without_nvidia_is_error()
    {
        var input = BaseInput() with { Backend = RuntimeBackend.Cuda, HasNvidiaGpu = false };
        var findings = SettingsConflictEngine.Check(input);
        Assert.True(Has(findings, ConflictSeverity.Error, "CUDA"));
    }

    [Fact]
    public void Memory_overflow_produces_error()
    {
        var ramOnly = new DeviceMemoryPlan(
            new[] { new DeviceMemoryRow("RAM", MemoryDeviceKind.SystemRam, 16.0, 4.0, 40.0) },
            TotalModelGb: 40.0,
            OverflowGb: 28.0);
        var input = BaseInput() with { MemoryPlan = ramOnly };
        var findings = SettingsConflictEngine.Check(input);
        Assert.True(Has(findings, ConflictSeverity.Error, "память"));
    }
}
