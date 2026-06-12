using Launcher.Online;

namespace Launcher.Online.Tests;

public class AnthropicStreamParserTests
{
    [Fact]
    public void Extracts_text_delta()
    {
        var ok = AnthropicStreamParser.TryParseLine(
            "data: {\"type\":\"content_block_delta\",\"delta\":{\"type\":\"text_delta\",\"text\":\"Hi\"}}",
            out var content, out var done);

        Assert.True(ok);
        Assert.False(done);
        Assert.Equal("Hi", content);
    }

    [Fact]
    public void Recognizes_message_stop()
    {
        var ok = AnthropicStreamParser.TryParseLine(
            "data: {\"type\":\"message_stop\"}", out var content, out var done);

        Assert.True(ok);
        Assert.True(done);
        Assert.Null(content);
    }

    [Fact]
    public void Ignores_event_lines()
    {
        Assert.False(AnthropicStreamParser.TryParseLine("event: content_block_delta", out _, out _));
    }
}
