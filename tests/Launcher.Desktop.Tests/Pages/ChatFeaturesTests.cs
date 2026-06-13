using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Desktop.ViewModels.Pages;
using Launcher.Online;

namespace Launcher.Desktop.Tests.Pages;

public class ChatFeaturesTests
{
    private sealed class CapturingClient : IChatClient
    {
        public IReadOnlyList<ChatMessage>? Captured { get; private set; }

        public async IAsyncEnumerable<string> StreamAsync(
            ChatEndpoint endpoint, IReadOnlyList<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Captured = messages;
            await Task.Yield();
            yield return "ok";
        }
    }

    [Theory]
    [InlineData("Qwen3-30B-A3B-Q4_K_M.gguf", true)]
    [InlineData("Mixtral-8x7B-Instruct-Q4_K_M.gguf", true)]
    [InlineData("glm-4.6-q4_k_m.gguf", true)]
    [InlineData("qwen2.5-coder-7b-instruct-q4_k_m.gguf", false)]
    [InlineData("llama-3.1-8b-q4_k_m.gguf", false)]
    public void Detects_moe_models_by_name(string file, bool expected)
    {
        Assert.Equal(expected, ChatViewModel.IsLikelyMoE(file));
    }

    [Fact]
    public async Task System_prompt_is_prepended_to_request()
    {
        var client = new CapturingClient();
        var vm = new ChatViewModel(client) { SystemPrompt = "Будь краток.", Input = "привет" };

        await vm.SendCommand.ExecuteAsync(null);

        Assert.NotNull(client.Captured);
        Assert.Equal("system", client.Captured![0].Role);
        Assert.Equal("Будь краток.", client.Captured[0].Content);
        Assert.Contains(client.Captured, m => m.Role == "user" && m.Content == "привет");
    }

    [Fact]
    public void Response_styles_include_precise_and_creative()
    {
        var names = ResponseStyle.All.Select(s => s.Name).ToList();
        Assert.Contains("Точный (код)", names);
        Assert.Contains("Творческий", names);
        Assert.Contains("--xtc-probability", ResponseStyle.All.First(s => s.Name == "Творческий").Args);
    }
}
