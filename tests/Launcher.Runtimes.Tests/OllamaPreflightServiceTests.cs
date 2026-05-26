using Launcher.Runtimes.Ollama;

namespace Launcher.Runtimes.Tests;

public sealed class OllamaPreflightServiceTests
{
    [Fact]
    public async Task PassesWhenModelIsVisibleAndTinyGenerateWorks()
    {
        var client = new FakeOllamaClient(
            tags: ["qwen:latest"],
            openAiModels: ["qwen:latest"],
            generateOk: true);
        var service = new OllamaPreflightService(client);

        var result = await service.CheckAsync("qwen:latest", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Checks, check => check.Name == "Ollama /api/tags" && check.Success);
        Assert.Contains(result.Checks, check => check.Name == "Ollama /v1/models" && check.Success);
        Assert.Contains(result.Checks, check => check.Name == "Ollama /api/generate" && check.Success);
    }

    [Fact]
    public async Task FailsWhenOpenAiEndpointDoesNotExposeModel()
    {
        var client = new FakeOllamaClient(
            tags: ["qwen:latest"],
            openAiModels: [],
            generateOk: true);
        var service = new OllamaPreflightService(client);

        var result = await service.CheckAsync("qwen:latest", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, error => error.Contains("/v1/models", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeOllamaClient(
        IReadOnlyList<string> tags,
        IReadOnlyList<string> openAiModels,
        bool generateOk) : IOllamaRuntimeClient
    {
        public Task<IReadOnlyList<string>> ListTagsAsync(CancellationToken cancellationToken) => Task.FromResult(tags);

        public Task<IReadOnlyList<string>> ListOpenAiModelsAsync(CancellationToken cancellationToken) => Task.FromResult(openAiModels);

        public Task<bool> TinyGenerateAsync(string modelName, CancellationToken cancellationToken) => Task.FromResult(generateOk);
    }
}
