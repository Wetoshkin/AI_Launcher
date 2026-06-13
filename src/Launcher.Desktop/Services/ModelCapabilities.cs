using System;
using System.Collections.Generic;
using System.Linq;

namespace Launcher.Desktop.Services;

/// <summary>Возможности модели для отображения бейджами.</summary>
public sealed record ModelCapabilities(bool Tools, bool Vision, bool Reasoning)
{
    public static ModelCapabilities None { get; } = new(false, false, false);

    /// <summary>Короткие бейджи для UI, напр. «🛠 инструменты  👁 зрение».</summary>
    public string Badges
    {
        get
        {
            var parts = new List<string>();
            if (Tools) parts.Add("🛠 инструменты");
            if (Vision) parts.Add("👁 зрение");
            if (Reasoning) parts.Add("🧠 размышления");
            return string.Join("   ", parts);
        }
    }

    public bool Any => Tools || Vision || Reasoning;
}

/// <summary>
/// Определяет возможности модели по имени файла, шаблону чата (GGUF) и наличию mmproj.
/// Эвристики, но без сети — работают и для офлайновых моделей.
/// </summary>
public static class ModelCapabilityDetector
{
    public static ModelCapabilities Detect(string fileName, string? chatTemplate, bool hasMmproj, IEnumerable<string>? tags = null)
    {
        var name = (fileName ?? string.Empty).ToLowerInvariant();
        var tpl = (chatTemplate ?? string.Empty).ToLowerInvariant();
        var tagSet = tags?.Select(t => t.ToLowerInvariant()).ToArray() ?? Array.Empty<string>();

        var tools = tpl.Contains("tool_call") || tpl.Contains("tools") || tpl.Contains("function")
            || tagSet.Any(t => t.Contains("tool") || t.Contains("function-calling"));

        var vision = hasMmproj
            || ContainsAny(name, "-vl", "vl-", "vision", "llava", "moondream", "minicpm-v", "-vl_", "qwen2-vl", "qwen2.5-vl", "internvl")
            || tagSet.Any(t => t.Contains("vision") || t.Contains("multimodal") || t.Contains("image-text-to-text"));

        var reasoning = ContainsAny(name, "think", "reasoning", "-r1", "qwq", "deepseek-r1", "deephermes", "marco-o1")
            || ContainsAny(tpl, "<think>", "reasoning")
            || tagSet.Any(t => t.Contains("reasoning"));

        return new ModelCapabilities(tools, vision, reasoning);
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));
}
