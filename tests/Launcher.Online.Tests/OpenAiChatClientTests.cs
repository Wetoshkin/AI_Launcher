using System.Net;
using System.Text;
using Launcher.Online;

namespace Launcher.Online.Tests;

public class OpenAiChatClientTests
{
    private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/event-stream"),
            };
        }
    }

    [Fact]
    public async Task Streams_content_deltas_until_done()
    {
        var sse = string.Join("\n",
            "data: {\"choices\":[{\"delta\":{\"role\":\"assistant\"}}]}",
            "data: {\"choices\":[{\"delta\":{\"content\":\"При\"}}]}",
            "data: {\"choices\":[{\"delta\":{\"content\":\"вет\"}}]}",
            "data: [DONE]",
            "data: {\"choices\":[{\"delta\":{\"content\":\"後\"}}]}");
        var handler = new StubHandler(sse);
        var client = new OpenAiChatClient(new HttpClient(handler));

        var tokens = new List<string>();
        await foreach (var t in client.StreamAsync(
            new ChatEndpoint("http://127.0.0.1:8080/v1", "local/test"),
            new[] { new ChatMessage("user", "hi") },
            CancellationToken.None))
        {
            tokens.Add(t);
        }

        Assert.Equal(new[] { "При", "вет" }, tokens);
    }

    [Fact]
    public async Task Sends_api_key_and_model_in_request()
    {
        var handler = new StubHandler("data: [DONE]");
        var client = new OpenAiChatClient(new HttpClient(handler));

        await foreach (var _ in client.StreamAsync(
            new ChatEndpoint("https://api.example.com/v1", "gpt-x", "secret-key"),
            new[] { new ChatMessage("user", "hi") },
            CancellationToken.None))
        {
        }

        Assert.Equal("Bearer", handler.LastRequest!.Headers.Authorization!.Scheme);
        Assert.Equal("secret-key", handler.LastRequest.Headers.Authorization.Parameter);
        Assert.Contains("\"model\":\"gpt-x\"", handler.LastBody);
        Assert.Contains("https://api.example.com/v1/chat/completions", handler.LastRequest.RequestUri!.ToString());
    }
}
