using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Launcher.Desktop.Converters;

/// <summary>Цвет бейджа «влезает в память»: 0 — быстро (VRAM), 1 — медленнее (ОЗУ), 2 — не влезет, 3 — неизвестно.</summary>
public static class FitConverters
{
    /// <summary>Зелёный, если модель запущена, иначе серый.</summary>
    public static readonly FuncValueConverter<bool, IBrush> RunningToBrush =
        new(running => new SolidColorBrush(Color.Parse(running ? "#2E9E5B" : "#8A8A90")));

    public static readonly FuncValueConverter<int, IBrush> LevelToBrush =
        new(level => new SolidColorBrush(Color.Parse(level switch
        {
            0 => "#2E9E5B",
            1 => "#C8881A",
            2 => "#D0524A",
            _ => "#8A8A90"
        })));
}
