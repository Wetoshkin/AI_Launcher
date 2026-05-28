using System.Net;
using Launcher.Runtimes.Startup;

namespace Launcher.Runtimes.Tests;

public sealed class OpenAiEndpointHealthClientTests
{
    [Fact]
    public async Task WaitUntilReadyAsyncReturnsReadyWhenModelsEndpointResponds()
    {
        var handler = new SequenceHttpHandler(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"local"}]}""")
            });
        var client = new OpenAiEndpointHealthClient(new HttpClient(handler));

        var result = await client.WaitUntilReadyAsync(
            "http://127.0.0.1:8080/v1",
            Attempts: 3,
            Delay: TimeSpan.Zero,
            CancellationToken.None);

        Assert.True(result.IsReady);
        Assert.Equal(2, result.Attempts);
        Assert.Equal("http://127.0.0.1:8080/v1/models", handler.RequestUris.Last().ToString());
    }

    [Fact]
    public async Task WaitUntilReadyAsyncReturnsLastErrorWhenEndpointNeverResponds()
    {
        var client = new OpenAiEndpointHealthClient(new HttpClient(new SequenceHttpHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.InternalServerError))));

        var result = await client.WaitUntilReadyAsync(
            "http://127.0.0.1:8080/v1",
            Attempts: 2,
            Delay: TimeSpan.Zero,
            CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.Equal(2, result.Attempts);
        Assert.Contains("500", result.Message);
    }

    private sealed class SequenceHttpHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private int _index;

        public List<Uri> RequestUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            var response = responses[Math.Min(_index, responses.Length - 1)];
            _index++;
            return Task.FromResult(response);
        }
    }
}
