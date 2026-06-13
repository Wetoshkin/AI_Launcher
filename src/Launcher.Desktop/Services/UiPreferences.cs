using System;
using System.IO;
using System.Text.Json;

namespace Launcher.Desktop.Services;

/// <summary>Сохраняемые настройки интерфейса: тема и язык. Хранятся в %LOCALAPPDATA%.</summary>
public sealed class UiPreferences
{
    public string Theme { get; set; } = "Light";   // Light | Dark | System
    public string Language { get; set; } = "ru";    // ru | en
    public bool UseIntegratedGpu { get; set; }       // учитывать встройку под LLM (по умолчанию нет)
    public string ModelsFolder { get; set; } = string.Empty; // запомненная папка с GGUF-моделями
    public System.Collections.Generic.List<string> RecentModels { get; set; } = new(); // частые модели (свежие первыми)
    public LaunchProfile? LastLaunch { get; set; }   // последний запуск для быстрого повтора

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
