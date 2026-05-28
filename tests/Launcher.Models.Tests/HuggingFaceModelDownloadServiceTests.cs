using System.Net;
using System.Text;
using Launcher.Models.HuggingFace;

namespace Launcher.Models.Tests;

public sealed class HuggingFaceModelDownloadServiceTests
{
    [Fact]
    public async Task DownloadAsyncWritesAllOptionFilesUnderRepoFolder()
    {
        using var temp = new TempDirectory();
        var option = new HuggingFaceGgufDownloadOption(
            "Qwen3-Coder-Q4_K_M.gguf",
            "Q4_K_M",
            IsSplit: false,
            Files:
            [
                new HuggingFaceGgufFile(
                    "quant/Qwen3-Coder-Q4_K_M.gguf",
                    "https://huggingface.co/unsloth/Qwen3-Coder-GGUF/resolve/main/quant/Qwen3-Coder-Q4_K_M.gguf",
                    IsFirstSplitShard: true)
            ]);
        var handler = new FakeDownloadHandler(("https://huggingface.co/unsloth/Qwen3-Coder-GGUF/resolve/main/quant/Qwen3-Coder-Q4_K_M.gguf", "model-bytes"));
        var service = new HuggingFaceModelDownloadService(new HttpClient(handler));

        var result = await service.DownloadAsync(
            new HuggingFaceModelDownloadRequest("unsloth/Qwen3-Coder-GGUF", option, temp.Path),
            CancellationToken.None);

        var targetPath = Path.Combine(temp.Path, "unsloth", "Qwen3-Coder-GGUF", "quant", "Qwen3-Coder-Q4_K_M.gguf");
        Assert.True(File.Exists(targetPath));
        Assert.Equal("model-bytes", File.ReadAllText(targetPath));
        Assert.Equal([targetPath], result.DownloadedFiles);
        Assert.Empty(result.SkippedFiles);
    }

    [Fact]
    public async Task DownloadAsyncSkipsExistingFilesAndReportsProgress()
    {
        using var temp = new TempDirectory();
        var existingPath = Path.Combine(temp.Path, "bartowski", "DeepSeek-R1-GGUF", "DeepSeek-R1-Q4_K_M-00001-of-00002.gguf");
        Directory.CreateDirectory(Path.GetDirectoryName(existingPath)!);
        File.WriteAllText(existingPath, "already-here");
        var option = new HuggingFaceGgufDownloadOption(
            "DeepSeek-R1-Q4_K_M.gguf",
            "Q4_K_M",
            IsSplit: true,
            Files:
            [
                new HuggingFaceGgufFile("DeepSeek-R1-Q4_K_M-00001-of-00002.gguf", "https://hf/part1", IsFirstSplitShard: true),
                new HuggingFaceGgufFile("DeepSeek-R1-Q4_K_M-00002-of-00002.gguf", "https://hf/part2", IsFirstSplitShard: false)
            ]);
        var progress = new List<HuggingFaceDownloadProgress>();
        var service = new HuggingFaceModelDownloadService(new HttpClient(new FakeDownloadHandler(("https://hf/part2", "second-part"))));

        var result = await service.DownloadAsync(
            new HuggingFaceModelDownloadRequest("bartowski/DeepSeek-R1-GGUF", option, temp.Path),
            CancellationToken.None,
            progress.Add);

        Assert.Equal([existingPath], result.SkippedFiles);
        Assert.Single(result.DownloadedFiles);
        Assert.Equal("already-here", File.ReadAllText(existingPath));
        Assert.Contains(progress, item => item.FileName.EndsWith("00001-of-00002.gguf", StringComparison.Ordinal) && item.IsSkipped);
        Assert.Contains(progress, item => item.FileName.EndsWith("00002-of-00002.gguf", StringComparison.Ordinal) && !item.IsSkipped);
    }

    [Fact]
    public async Task DownloadAsyncRejectsPathTraversalFileNames()
    {
        using var temp = new TempDirectory();
        var option = new HuggingFaceGgufDownloadOption(
            "bad.gguf",
            "Q4_K_M",
            IsSplit: false,
            Files:
            [
                new HuggingFaceGgufFile("../bad.gguf", "https://hf/bad", IsFirstSplitShard: true)
            ]);
        var service = new HuggingFaceModelDownloadService(new HttpClient(new FakeDownloadHandler(("https://hf/bad", "bad"))));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DownloadAsync(
            new HuggingFaceModelDownloadRequest("owner/repo", option, temp.Path),
            CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsyncReportsIntermediateByteProgress()
    {
        using var temp = new TempDirectory();
        var option = new HuggingFaceGgufDownloadOption(
            "chunked.gguf",
            "Q4_K_M",
            IsSplit: false,
            Files:
            [
                new HuggingFaceGgufFile("chunked.gguf", "https://hf/chunked", IsFirstSplitShard: true)
            ]);
        var progress = new List<HuggingFaceDownloadProgress>();
        var service = new HuggingFaceModelDownloadService(new HttpClient(new ChunkedDownloadHandler("https://hf/chunked", new byte[200_000])));

        await service.DownloadAsync(
            new HuggingFaceModelDownloadRequest("owner/repo", option, temp.Path),
            CancellationToken.None,
            progress.Add);

        Assert.Contains(progress, item => item.BytesReceived > 0 && item.BytesReceived < 200_000);
        Assert.Contains(progress, item => item.BytesReceived == 200_000 && item.TotalBytes == 200_000);
    }

    private sealed class FakeDownloadHandler(params (string Url, string Body)[] responses) : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses = responses.ToDictionary(item => item.Url, item => item.Body);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!_responses.TryGetValue(request.RequestUri!.ToString(), out var body))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/octet-stream")
            });
        }
    }

    private sealed class ChunkedDownloadHandler(string url, byte[] body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.ToString() != url)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(body)
                {
                    Headers =
                    {
                        ContentLength = body.LongLength
                    }
                }
            });
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "launcher-models-" + Guid.NewGuid().ToString("N"));

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
