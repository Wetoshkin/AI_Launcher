using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Launcher.Desktop.Services;

/// <summary>
/// Достаёт из README модели рекомендованные автором параметры сэмплинга для llama-server.
/// Берёт флаги из примеров команд (llama-server/llama-cli) и понятные упоминания в тексте
/// (temperature/top_p/top_k/min_p). Инфраструктурные флаги (модель, порт, контекст) отбрасываются —
/// их задаёт само приложение.
/// </summary>
public static class HfReadmeParser
{
    // Флаги, которые мы выставляем сами — из рекомендаций их исключаем.
    private static readonly HashSet<string> InfraFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "-m", "--model", "-hf", "--hf-repo", "--hf-file", "--hf-repo-v", "--hf-file-v",
        "--port", "--host", "--alias", "-c", "--ctx-size", "-ngl", "--n-gpu-layers",
        "-t", "--threads", "--threads-batch", "-tb", "--tensor-split", "-ts",
        "--split-mode", "-sm", "--no-mmap", "--mlock", "-fa", "--flash-attn",
        "--cache-type-k", "-ctk", "--cache-type-v", "-ctv", "-b", "--batch-size",
        "-ub", "--ubatch-size", "--n-cpu-moe", "--cpu-moe", "-v", "--verbose",
    };

    public static string? ExtractRecommendedArgs(string? readme)
    {
        if (string.IsNullOrWhiteSpace(readme))
        {
            return null;
        }

        var args = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in readme.Split('\n'))
        {
            if (!MentionsLlama(line))
            {
                continue;
            }

            var tokens = Tokenize(line);
            for (var i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (!t.StartsWith('-') || t.Length < 2)
                {
                    continue;
                }

                if (InfraFlags.Contains(t))
                {
                    // пропустить и его значение, если есть
                    if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith('-'))
                    {
                        i++;
                    }

                    continue;
                }

                var flag = t;
                string? value = null;
                if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith('-'))
                {
                    value = tokens[i + 1];
                    i++;
                }

                if (seen.Add(flag))
                {
                    args.Add(value is null ? flag : $"{flag} {value}");
                }
            }
        }

        // Из прозы: «temperature 0.6», «top_p = 0.95», «top_k: 20», «min_p 0.05».
        AddFromProse(readme, "temperature", "--temp", args, seen);
        AddFromProse(readme, "top[_ -]?p", "--top-p", args, seen);
        AddFromProse(readme, "top[_ -]?k", "--top-k", args, seen);
        AddFromProse(readme, "min[_ -]?p", "--min-p", args, seen);

        return args.Count == 0 ? null : string.Join(" ", args);
    }

    private static bool MentionsLlama(string line) =>
        line.Contains("llama-server", StringComparison.OrdinalIgnoreCase)
        || line.Contains("llama-cli", StringComparison.OrdinalIgnoreCase)
        || line.Contains("llama.cpp", StringComparison.OrdinalIgnoreCase)
        || line.Contains("./server", StringComparison.OrdinalIgnoreCase)
        || line.Contains("./main", StringComparison.OrdinalIgnoreCase);

    private static List<string> Tokenize(string line) =>
        line.Split(new[] { ' ', '\t', '\\', '`' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().Trim('"', '\''))
            .Where(s => s.Length > 0)
            .ToList();

    private static void AddFromProse(string readme, string keyPattern, string flag, List<string> args, HashSet<string> seen)
    {
        if (seen.Contains(flag))
        {
            return;
        }

        // Допускаем короткую связку между ключом и числом: «= 0.6», «of 0.95», «: 20», «is 0.9».
        var m = Regex.Match(readme, keyPattern + @"\b[^0-9\n]{0,10}?([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);
        if (!m.Success || !double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
        {
            return;
        }

        var ok = flag switch
        {
            "--temp" => v > 0 && v <= 2,
            "--top-p" => v > 0 && v <= 1,
            "--min-p" => v > 0 && v <= 1,
            "--top-k" => v >= 1 && v <= 1000 && m.Groups[1].Value.IndexOf('.') < 0,
            _ => v > 0,
        };

        if (ok)
        {
            seen.Add(flag);
            args.Add($"{flag} {m.Groups[1].Value}");
        }
    }
}
