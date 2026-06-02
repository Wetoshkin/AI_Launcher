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

    [Fact]
    public async Task SearchPreservesSiblingFileSizesFromApiMetadata()
    {
        var handler = new FakeHttpHandler("""
        [
          {
            "id": "bartowski/Model-GGUF",
            "downloads": 10,
            "likes": 2,
            "tags": ["gguf"],
            "siblings": [
              {"rfilename": "Model-Q4_K_M.gguf", "size": 4294967296},
              {"rfilename": "Model-Q5_K_M.gguf", "sizeBytes": 5368709120},
              {"rfilename": "Model-Q6_K.gguf", "lfs": {"size": 6442450944}},
              {"rfilename": "Model-Q8_0.gguf"}
            ]
          }
        ]
        """);
        var client = new HuggingFaceModelClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://huggingface.co")
        });

        var result = Assert.Single(await client.SearchAsync(
            new HuggingFaceModelSearchRequest("model", HuggingFaceSort.Downloads, Limit: 10, GgufOnly: true),
            CancellationToken.None));

        Assert.Collection(result.SiblingFileMetadata!,
            file =>
            {
                Assert.Equal("Model-Q4_K_M.gguf", file.FileName);
                Assert.Equal(4_294_967_296, file.SizeBytes);
            },
            file =>
            {
                Assert.Equal("Model-Q5_K_M.gguf", file.FileName);
                Assert.Equal(5_368_709_120, file.SizeBytes);
            },
            file =>
            {
                Assert.Equal("Model-Q6_K.gguf", file.FileName);
                Assert.Equal(6_442_450_944, file.SizeBytes);
            },
            file =>
            {
                Assert.Equal("Model-Q8_0.gguf", file.FileName);
                Assert.Null(file.SizeBytes);
            });
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
