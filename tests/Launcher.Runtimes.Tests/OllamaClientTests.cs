using System.Net;
using System.Text;
using Launcher.Runtimes.Ollama;

namespace Launcher.Runtimes.Tests;

public sealed class OllamaClientTests
{
    [Fact]
    public async Task ListsTagsFromOllamaApi()
    {
        var http = new FakeHttpClient(new Dictionary<string, string>
        {
            ["/api/tags"] = """
            {"models":[{"name":"qwen-local:latest"},{"name":"gemma:latest"}]}
            """
        });
        var client = new OllamaClient(http, new Uri("http://127.0.0.1:11434"));

        var models = await client.ListTagsAsync(CancellationToken.None);

        Assert.Equal(["qwen-local:latest", "gemma:latest"], models);
    }

    [Fact]
    public async Task ListsOpenAiModelsFromOllamaV1()
    {
        var http = new FakeHttpClient(new Dictionary<string, string>
        {
            ["/v1/models"] = """
            {"data":[{"id":"qwen-local:latest"},{"id":"gemma:latest"}]}
            """
        });
        var client = new OllamaClient(http, new Uri("http://127.0.0.1:11434"));

        var models = await client.ListOpenAiModelsAsync(CancellationToken.None);

        Assert.Equal(["qwen-local:latest", "gemma:latest"], models);
    }

    [Fact]
    public async Task SendsTinyGeneratePreflight()
    {
        var http = new FakeHttpClient(new Dictionary<string, string>
        {
            ["/api/generate"] = """{"response":"ok","done":true}"""
        });
        var client = new OllamaClient(http, new Uri("http://127.0.0.1:11434"));

        var ok = await client.TinyGenerateAsync("qwen-local:latest", CancellationToken.None);

        Assert.True(ok);
        Assert.Contains("\"model\":\"qwen-local:latest\"", http.LastBody);
        Assert.Contains("\"stream\":false", http.LastBody);
    }

    private sealed class FakeHttpClient(IReadOnlyDictionary<string, string> responses) : IOllamaHttpClient
    {
        public string LastBody { get; private set; } = "";

        public Task<string> GetStringAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (responses.TryGetValue(uri.AbsolutePath, out var response))
            {
                return Task.FromResult(response);
            }

            throw new HttpRequestException("Not found", null, HttpStatusCode.NotFound);
        }

        public Task<string> PostJsonAsync(Uri uri, string json, CancellationToken cancellationToken)
        {
            LastBody = json;
            if (responses.TryGetValue(uri.AbsolutePath, out var response))
            {
                return Task.FromResult(response);
            }

            throw new HttpRequestException("Not found", null, HttpStatusCode.NotFound);
        }
    }
}
