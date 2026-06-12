using System;
using System.IO;
using System.Text.Json;

namespace Launcher.Desktop.Services;

/// <summary>Сохраняемые настройки интерфейса: тема и язык. Хранятся в %LOCALAPPDATA%.</summary>
public sealed class UiPreferences
{
    public string Theme { get; set; } = "Light";   // Light | Dark | System
    public string Language { get; set; } = "ru";    // ru | en

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AILauncherStudio", "ui-settings.json");

    public static UiPreferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<UiPreferences>(json) ?? new UiPreferences();
            }
        }
        catch
        {
            // повреждённый файл — вернём дефолт
        }

        return new UiPreferences();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // не критично
        }
    }
}
