namespace Launcher.Desktop.Services;

/// <summary>Снимок последнего запуска: модель, параметры, агент и проект — для быстрого повтора.</summary>
public sealed class LaunchProfile
{
    public string ModelPath { get; set; } = string.Empty;
    public int ContextTokens { get; set; } = 32768;
    public int KvModeIndex { get; set; } = 1;
    public string Agent { get; set; } = "OpenCode";
    public string ProjectFolder { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public bool MoeAuto { get; set; } = true;
    public int MoeCpuLayers { get; set; }
    public bool Reasoning { get; set; }
    public int ReasoningDepth { get; set; }
    public bool FlashAttention { get; set; }
    public bool KeepInRam { get; set; }
    public bool NoMmap { get; set; }
    public bool SplitModeRow { get; set; }
    public bool VerboseLog { get; set; }
    public string ExpertArgs { get; set; } = string.Empty;
    public int Port { get; set; } = 8080;

    public bool HasValue => !string.IsNullOrWhiteSpace(ModelPath) || !string.IsNullOrWhiteSpace(ProjectFolder);

    /// <summary>Короткое описание для кнопки быстрого повтора.</summary>
    public string Summary
    {
        get
        {
            var model = string.IsNullOrWhiteSpace(ModelPath)
                ? "онлайн"
                : System.IO.Path.GetFileNameWithoutExtension(ModelPath);
            var folder = string.IsNullOrWhiteSpace(ProjectFolder)
                ? string.Empty
                : " · " + System.IO.Path.GetFileName(ProjectFolder.TrimEnd('/', '\\'));
            return $"{model} · {Agent}{folder}";
        }
    }
}
