using System.Collections.Generic;

namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>
/// Размер контекста (память диалога). Для кодинг-агентов нужен большой контекст —
/// они шлют системный промпт и куски файлов, иначе модель «компактит» историю и не успевает ответить.
/// </summary>
public sealed record LaunchPreset(string Name, int ContextTokens, string Hint)
{
    public override string ToString() => Name;

    public static IReadOnlyList<LaunchPreset> All { get; } = new[]
    {
        new LaunchPreset("8K — короткие задачи", 8192,
            "Маленький контекст. Для простого чата хватит, но агентам обычно мало."),
        new LaunchPreset("16K — обычные задачи", 16384,
            "Средний контекст. Подходит для несложных задач с агентом."),
        new LaunchPreset("32K — агенты (рекомендуется)", 32768,
            "Рекомендуется для кодинг-агентов: помнит системный промпт и контекст проекта."),
        new LaunchPreset("64K — большой контекст", 65536,
            "Большие проекты. Нужно заметно больше видеопамяти под KV-кэш."),
        new LaunchPreset("128K — максимум", 131072,
            "Максимальный контекст. Ест много памяти — следите за прогнозом памяти."),
    };

    /// <summary>По умолчанию — 32K: оптимально для кодинг-агентов.</summary>
    public static LaunchPreset Default => All[2];
}
