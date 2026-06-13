using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Launcher.Desktop.Services;

/// <summary>Кэшированные сведения о локальной модели (рядом с .gguf, чтобы не перечитывать файл).</summary>
public sealed class CachedModelInfo
{
    public int Context { get; set; }
    public bool Tools { get; set; }
    public bool Vision { get; set; }
    public bool Reasoning { get; set; }
    public long FileLength { get; set; }
    public long Modified { get; set; }
    public string? RecommendedArgs { get; set; }
}

/// <summary>
/// Читает/пишет сведения о модели в sidecar-файл «&lt;модель&gt;.alinfo.json».
/// Если sidecar актуален (совпадает размер и дата файла) — возвращает его без чтения GGUF.
/// </summary>
public static class ModelInfoStore
{
    private static string SidecarPath(string modelPath) => modelPath + ".alinfo.json";

    public static CachedModelInfo Get(string modelPath)
    {
        var fi = new FileInfo(modelPath);
        var sidecar = SidecarPath(modelPath);

        if (TryLoad(sidecar) is { } cached
            && cached.FileLength == fi.Length
            && cached.Modified == fi.LastWriteTimeUtc.Ticks)
        {
            return cached;
        }

        var info = GgufMetadata.ReadInfo(modelPath);
        var hasMmproj = HasSiblingMmproj(modelPath);
        var caps = ModelCapabilityDetector.Detect(Path.GetFileName(modelPath), info.ChatTemplate, hasMmproj);

        var result = new CachedModelInfo
        {
            Context = info.ContextLength ?? 0,
            Tools = caps.Tools,
            Vision = caps.Vision,
            Reasoning = caps.Reasoning,
            FileLength = fi.Length,
            Modified = fi.LastWriteTimeUtc.Ticks,
            RecommendedArgs = TryLoad(sidecar)?.RecommendedArgs, // сохраняем уже скачанные рекомендации
        };

        Save(sidecar, result);
        return result;
    }

    /// <summary>Сохранить рекомендованные автором параметры запуска рядом с моделью.</summary>
    public static void SaveRecommendedArgs(string modelPath, string args)
    {
        try
        {
            var info = Get(modelPath);
            info.RecommendedArgs = args;
            Save(SidecarPath(modelPath), info);
        }
        catch
        {
            // не критично
        }
    }

    public static string? GetRecommendedArgs(string modelPath)
    {
        try
        {
            return TryLoad(SidecarPath(modelPath))?.RecommendedArgs;
        }
        catch
        {
            return null;
        }
    }

    private static CachedModelInfo? TryLoad(string sidecar)
    {
        try
        {
            if (File.Exists(sidecar))
            {
                return JsonSerializer.Deserialize<CachedModelInfo>(File.ReadAllText(sidecar));
            }
        }
        catch
        {
            // повреждённый sidecar
        }

        return null;
    }

    private static void Save(string sidecar, CachedModelInfo info)
    {
        try
        {
            File.WriteAllText(sidecar, JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // папка только для чтения — не критично
        }
    }

    private static bool HasSiblingMmproj(string modelPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(modelPath);
            if (string.IsNullOrEmpty(dir))
            {
                return false;
            }

            return Directory.EnumerateFiles(dir, "*.gguf")
                .Any(f => Path.GetFileName(f).Contains("mmproj", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
