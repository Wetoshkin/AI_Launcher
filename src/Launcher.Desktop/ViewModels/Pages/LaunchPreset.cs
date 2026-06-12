using System.Collections.Generic;

namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>
/// Пресет запуска для новичка. Приоритет: удобный контекст → стабильность → качество → скорость.
/// </summary>
public sealed record LaunchPreset(string Name, int ContextTokens, string Hint)
{
    public override string ToString() => Name;

    public static IReadOnlyList<LaunchPreset> All { get; } = new[]
    {
        new LaunchPreset("Быстро", 2048,
            "Маленький контекст (2K). Меньше памяти и быстрее ответы, но модель помнит меньше."),
        new LaunchPreset("Сбалансированно", 4096,
            "Средний контекст (4K). Хороший баланс памяти, стабильности и скорости для большинства задач."),
        new LaunchPreset("Качество", 8192,
            "Большой контекст (8K). Модель помнит больше, но нужно больше памяти и ответы чуть медленнее."),
    };

    public static LaunchPreset Default => All[1];
}
