using System.Net;
using System.Text;
using Launcher.Online;

namespace Launcher.Online.Tests;

public class AnthropicChatClientTests
{
    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/event-stream"),
            };
        }
    }

    [Fact]
    public async Task Streams_text_deltas_until_message_stop()
    {
        var sse = string.Join("\n",
            "event: content_block_delta",
            "data: {\"type\":\"content_block_delta\",\"delta\":{\"type\":\"text_delta\",\"text\":\"При\"}}",
            "event: content_block_delta",
            "data: {\"type\":\"content_block_delta\",\"delta\":{\"type\":\"text_delta\",\"text\":\"вет\"}}",
            "event: message_stop",
            "data: {\"type\":\"message_stop\"}");
        var handler = new StubHandler(sse);
        var client = new AnthropicChatClient(new HttpClient(handler));

        var tokens = new List<string>();
        await foreach (var t in client.StreamAsync(
            new ChatEndpoint("https://api.anthropic.com/v1", "claude-3-5-haiku-latest", "key"),
            new[] { new ChatMessage("system", "будь краток"), new ChatMessage("user", "привет") },
            CancellationToken.None))
        {
            tokens.Add(t);
        }

        Assert.Equal(new[] { "При", "вет" }, tokens);
        Assert.Equal("key", handler.LastRequest!.Headers.GetValues("x-api-key").Single());
        Assert.Contains("\"system\":\"будь краток\"", handler.LastBody);
        Assert.Contains("api.anthropic.com/v1/messages", handler.LastRequest.RequestUri!.ToString());
    }
}
