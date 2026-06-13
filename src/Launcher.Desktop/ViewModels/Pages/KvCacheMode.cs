using System.Collections.Generic;

namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>
/// Сжатие KV-кэша («турбоквант» памяти под контекст). Чем сильнее сжатие — тем больше
/// контекста влезет в ту же видеопамять, но чуть ниже точность. Factor — множитель памяти кэша.
/// </summary>
public sealed record KvCacheMode(string Name, string? Args, double Factor)
{
    public override string ToString() => Name;

    // Flash Attention добавляется отдельно в ComposeServerArgs (чтобы не дублировать флаг).
    public static IReadOnlyList<KvCacheMode> All { get; } = new[]
    {
        new KvCacheMode("Полный (f16) — макс. точность", null, 1.0),
        new KvCacheMode("Половина (q8_0) — рекомендуется",
            "--cache-type-k q8_0 --cache-type-v q8_0", 0.5),
        new KvCacheMode("Четверть (q4_0) — максимум контекста",
            "--cache-type-k q4_0 --cache-type-v q4_0", 0.25),
    };

    /// <summary>Сжатый KV-кэш требует Flash Attention.</summary>
    public bool RequiresFlashAttention => Factor < 1.0;

    /// <summary>По умолчанию — q8_0: вдвое меньше памяти под контекст почти без потери качества.</summary>
    public static KvCacheMode Default => All[1];
}
