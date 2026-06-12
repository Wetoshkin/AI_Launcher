using Launcher.Online;

namespace Launcher.Online.Tests;

public class OpenAiStreamParserTests
{
    [Fact]
    public void Extracts_content_delta_from_data_line()
    {
        var ok = OpenAiStreamParser.TryParseLine(
            "data: {\"choices\":[{\"delta\":{\"content\":\"Привет\"}}]}",
            out var content, out var done);

        Assert.True(ok);
        Assert.False(done);
        Assert.Equal("Привет", content);
    }

    [Fact]
    public void Recognizes_done_sentinel()
    {
        var ok = OpenAiStreamParser.TryParseLine("data: [DONE]", out var content, out var done);

        Assert.True(ok);
        Assert.True(done);
        Assert.Null(content);
    }

    [Fact]
    public void Ignores_non_data_and_empty_lines()
    {
        Assert.False(OpenAiStreamParser.TryParseLine("", out _, out _));
        Assert.False(OpenAiStreamParser.TryParseLine(": keep-alive", out _, out _));
        Assert.False(OpenAiStreamParser.TryParseLine("event: ping", out _, out _));
    }

    [Fact]
    public void Returns_true_with_null_content_when_delta_has_no_content()
    {
        var ok = OpenAiStreamParser.TryParseLine(
            "data: {\"choices\":[{\"delta\":{\"role\":\"assistant\"}}]}",
            out var content, out var done);

        Assert.True(ok);
        Assert.False(done);
        Assert.Null(content);
    }
}
