using System.Text.Json;

namespace Launcher.Online;

/// <summary>
/// Разбирает SSE-поток Anthropic Messages API (stream=true).
/// Значимы строки "data: {json}" с type=content_block_delta (delta.text) и type=message_stop.
/// </summary>
public static class AnthropicStreamParser
{
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

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (!doc.RootElement.TryGetProperty("type", out var typeElement))
            {
                return true;
            }

            var type = typeElement.GetString();
            if (type == "message_stop")
            {
                done = true;
                return true;
            }

            if (type == "content_block_delta"
                && doc.RootElement.TryGetProperty("delta", out var delta)
                && delta.TryGetProperty("text", out var textElement)
                && textElement.ValueKind == JsonValueKind.String)
            {
                content = textElement.GetString();
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
