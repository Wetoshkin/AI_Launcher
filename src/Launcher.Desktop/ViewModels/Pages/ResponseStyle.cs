using System.Collections.Generic;

namespace Launcher.Desktop.ViewModels.Pages;

/// <summary>
/// Стиль ответов = набор сэмплер-аргументов llama-server. Переводит выводы ресёрча
/// (temp/top-k/top-p/XTC + DRY уже включён) в понятный выбор для новичка.
/// </summary>
public sealed record ResponseStyle(string Name, string Args)
{
    public override string ToString() => Name;

    public static IReadOnlyList<ResponseStyle> All { get; } = new[]
    {
        new ResponseStyle("Сбалансированный", string.Empty),
        new ResponseStyle("Точный (код)", "--temp 0.6 --top-k 20 --top-p 0.95"),
        new ResponseStyle("Творческий", "--temp 1.0 --min-p 0.05 --xtc-probability 0.5"),
    };
}
