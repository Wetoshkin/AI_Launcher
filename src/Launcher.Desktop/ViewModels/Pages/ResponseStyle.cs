using System.Collections.Generic;

namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>
/// Стиль ответов = набор сэмплер-аргументов llama-server. Переводит выводы ресёрча
/// (temp/top-k/top-p/XTC + DRY уже включён) в понятный выбор для новичка.
/// </summary>
public sealed record ResponseStyle(string Name, string Args)
{
    public override string ToString() => Display;

    /// <summary>Имя с указанием температуры — чтобы было видно, что зашито в пресет.</summary>
    public string Display
    {
        get
        {
            var m = System.Text.RegularExpressions.Regex.Match(Args, @"--temp\s+([0-9.]+)");
            return m.Success ? $"{Name} · t={m.Groups[1].Value}" : $"{Name} · t≈0.8 (по умолчанию)";
        }
    }

    public static IReadOnlyList<ResponseStyle> All { get; } = new[]
    {
        new ResponseStyle("Сбалансированный", string.Empty),
        new ResponseStyle("Точный (код)", "--temp 0.6 --top-k 20 --top-p 0.95"),
        new ResponseStyle("Творческий", "--temp 1.0 --min-p 0.05 --xtc-probability 0.5"),
    };
}
