using System.Text.Json;

namespace Launcher.Online;

/// <summary>
/// Разбирает одну строку SSE-потока OpenAI-совместимого чата (/v1/chat/completions, stream=true).
/// Строки вида "data: {json}" с дельтой контента, либо "data: [DONE]" — конец потока.
/// </summary>
public static class OpenAiStreamParser
{
    /// <returns>true, если строка — значимое событие потока (данные или [DONE]); иначе false.</returns>
    public static bool TryParseLine(string line, out string? content, out bool done)
    {
        content = null;
        done = false;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith("data:", StringComparison.Ordinal))
        {
            return false;
        }

        var payload = trimmed["data:".Length..].Trim();
        if (payload.Length == 0)
        {
            return false;
        }

        if (payload == "[DONE]")
        {
            done = true;
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("choices", out var choices)
                && choices.ValueKind == JsonValueKind.Array
                && choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("delta", out var delta)
                    && delta.TryGetProperty("content", out var contentElement)
                    && contentElement.ValueKind == JsonValueKind.String)
                {
                    content = contentElement.GetString();
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
