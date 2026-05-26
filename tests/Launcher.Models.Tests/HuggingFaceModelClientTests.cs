using System.Net;
using Launcher.Models.HuggingFace;

namespace Launcher.Models.Tests;

public sealed class HuggingFaceModelClientTests
{
    [Fact]
    public async Task SearchBuildsModelsApiQueryWithSortAndParsesMetadata()
    {
        var handler = new FakeHttpHandler("""
        [
          {
            "id": "unsloth/Qwen3-Coder-GGUF",
            "downloads": 1234567,
            "likes": 900,
            "tags": ["gguf", "qwen", "text-generation"],
            "siblings": [{"rfilename": "Qwen3-Coder-Q4_K_M.gguf"}]
          }
        ]
        """);
        var client = new HuggingFaceModelClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://huggingface.co")
        });

        var results = await client.SearchAsync(
            new HuggingFaceModelSearchRequest("qwen coder", HuggingFaceSort.Downloads, Limit: 20, GgufOnly: true),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("unsloth/Qwen3-Coder-GGUF", results[0].Id);
        Assert.Equal(1_234_567, results[0].Downloads);
        Assert.Equal(900, results[0].Likes);
        Assert.Contains("gguf", results[0].Tags);
        Assert.NotNull(results[0].SiblingFiles);
        Assert.Contains("Qwen3-Coder-Q4_K_M.gguf", results[0].SiblingFiles!);
        Assert.Equal("/api/models", handler.LastRequestUri!.AbsolutePath);
        Assert.Contains("search=qwen%20coder", handler.LastRequestUri.Query);
        Assert.Contains("sort=downloads", handler.LastRequestUri.Query);
        Assert.Contains("filter=gguf", handler.LastRequestUri.Query);
        Assert.Contains("full=true", handler.LastRequestUri.Query);
    }

    private sealed class FakeHttpHandler(string responseBody) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
