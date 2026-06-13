using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests.Pages;

public class ChatMessageReasoningTests
{
    [Fact]
    public void Splits_completed_think_block()
    {
        var m = new ChatMessageViewModel("assistant", "<think>прикинул варианты</think>Ответ: 42", isUser: false);

        Assert.True(m.HasReasoning);
        Assert.Equal("прикинул варианты", m.Reasoning);
        Assert.Equal("Ответ: 42", m.Display);
    }

    [Fact]
    public void Streaming_think_block_shows_reasoning_in_progress()
    {
        var m = new ChatMessageViewModel("assistant", string.Empty, isUser: false);
        m.Append("<think>думаю");

        Assert.True(m.HasReasoning);
        Assert.Equal("думаю", m.Reasoning);
        Assert.Equal(string.Empty, m.Display);
    }

    [Fact]
    public void Plain_message_has_no_reasoning()
    {
        var m = new ChatMessageViewModel("assistant", "просто ответ", isUser: false);

        Assert.False(m.HasReasoning);
        Assert.Equal("просто ответ", m.Display);
    }
}
