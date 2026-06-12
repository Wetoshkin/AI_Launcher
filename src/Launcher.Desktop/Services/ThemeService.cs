using Avalonia;
using Avalonia.Styling;

namespace Launcher.Desktop.Services;

/// <summary>Переключает тему приложения (Светлая/Тёмная/Системная).</summary>
public static class ThemeService
{
    public static void Apply(string theme)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        app.RequestedThemeVariant = theme switch
        {
            "Dark" => ThemeVariant.Dark,
            "Light" => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };
    }
}
