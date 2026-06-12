using Launcher.Core.Scenarios;

namespace Launcher.Runtimes.Compatibility;

/// <summary>
/// Единая точка проверки совместимости настроек запуска. Разбирает реальные конфликты
/// (MTP ↔ модель/runtime, TurboQuant ↔ runtime, контекст ↔ нативный, KV ↔ runtime,
/// CUDA ↔ не-NVIDIA, нехватка памяти) и объясняет их простым языком.
/// Поглощает прежний RuntimeCompatibilityService.
/// </summary>
public static class SettingsConflictEngine
{
    public static IReadOnlyList<ConflictFinding> Check(ConflictCheckInput input)
    {
        var findings = new List<ConflictFinding>();

        CheckCudaBackend(input, findings);
        CheckMtp(input, findings);
        CheckTurboQuant(input, findings);
        CheckContext(input, findings);
        CheckMemory(input, findings);

        return findings;
    }

    private static void CheckCudaBackend(ConflictCheckInput input, List<ConflictFinding> findings)
    {
        if (input.Backend == RuntimeBackend.Cuda && !input.HasNvidiaGpu)
        {
            findings.Add(new ConflictFinding(
                ConflictSeverity.Error,
                "CUDA-сборка без видеокарты NVIDIA",
                "Выбрана сборка llama.cpp под CUDA, но в системе нет видеокарты NVIDIA — она не запустится.",
                "Выберите сборку под Vulkan (подходит Intel/AMD) или под CPU."));
        }
    }

    private static void CheckMtp(ConflictCheckInput input, List<ConflictFinding> findings)
    {
        if (!input.MtpEnabled)
        {
            return;
        }

        var runtimeSupports = input.Capabilities?.SupportsMtp
            ?? input.RuntimeKind == RuntimeKind.LlamaCppMtp;
        if (!runtimeSupports)
        {
            findings.Add(new ConflictFinding(
                ConflictSeverity.Error,
                "MTP не поддерживается этим runtime",
                "Включён MTP (предсказание нескольких токенов), но текущая сборка llama.cpp не умеет --spec-type draft-mtp.",
                "Скачайте MTP-сборку llama.cpp или выключите MTP."));
            return;
        }

        if (input.Model is { HasMtpHead: false })
        {
            findings.Add(new ConflictFinding(
                ConflictSeverity.Error,
                "У модели нет MTP-головы",
                $"MTP требует специальную модель (Qwen3.x MTP, Gemma4 MTP и т.п.). У «{input.Model.Name}» её нет.",
                "Выключите MTP или выберите модель с поддержкой MTP."));
            return;
        }

        findings.Add(new ConflictFinding(
            ConflictSeverity.Info,
            "MTP включён — проверьте выигрыш",
            "На совместимых моделях MTP ускоряет генерацию, но на части бэкендов даёт замедление.",
            "Сравните скорость с выключенным MTP; если медленнее — выключите."));
    }

    private static void CheckTurboQuant(ConflictCheckInput input, List<ConflictFinding> findings)
    {
        var wantsTurbo = input.KvCache.IsTurboQuant || input.RuntimeKind == RuntimeKind.LlamaCppTurboQuant;
        if (!wantsTurbo)
        {
            return;
        }

        var runtimeSupports = input.Capabilities?.SupportsTurboQuant
            ?? input.RuntimeKind == RuntimeKind.LlamaCppTurboQuant;
        if (!runtimeSupports)
        {
            findings.Add(new ConflictFinding(
                ConflictSeverity.Error,
                "TurboQuant не поддерживается этим runtime",
                "Выбран TurboQuant KV-кэш (turbo2/3/4), но обычная сборка llama.cpp его не понимает.",
                "Скачайте TurboQuant-сборку llama.cpp или выберите обычный KV-тип (например q8_0)."));
        }
    }

    private static void CheckContext(ConflictCheckInput input, List<ConflictFinding> findings)
    {
        if (input.Model?.NativeContextTokens is > 0 and var native && input.ContextTokens > native)
        {
            findings.Add(new ConflictFinding(
                ConflictSeverity.Warning,
                "Контекст больше, чем у модели",
                $"Модель обучена на {native} токенов, а задано {input.ContextTokens}. Сверх этого работает RoPE-масштабирование — возможна потеря качества на длинных текстах.",
                $"Верните контекст к {native} или меньше, если важна точность."));
        }
    }

    private static void CheckMemory(ConflictCheckInput input, List<ConflictFinding> findings)
    {
        if (input.MemoryPlan is null || input.MemoryPlan.Fits)
        {
            return;
        }

        findings.Add(new ConflictFinding(
            ConflictSeverity.Error,
            "Не помещается в память",
            $"Модель с текущими настройками не помещается: не хватает {input.MemoryPlan.OverflowGb:0.0} ГБ. " +
            "Запуск будет очень медленным или упадёт.",
            "Уменьшите контекст, возьмите модель меньше или более лёгкий KV-кэш (q8_0 → turbo4)."));
    }
}
