using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Desktop.ViewModels.Pages;
using Launcher.Online;

namespace Launcher.Desktop.Tests.Pages;

public class ChatViewModelTests
{
    private sealed class FakeChatClient : IChatClient
    {
        private readonly string[] _tokens;
        public FakeChatClient(params string[] tokens) => _tokens = tokens;

        public async IAsyncEnumerable<string> StreamAsync(
            ChatEndpoint endpoint,
            IReadOnlyList<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var token in _tokens)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return token;
                await Task.Yield();
            }
        }
    }

    private sealed class ThrowingChatClient : IChatClient
    {
        public async IAsyncEnumerable<string> StreamAsync(
            ChatEndpoint endpoint,
            IReadOnlyList<ChatMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new System.Net.Http.HttpRequestException("connection refused");
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }
    }

    [Fact]
    public async Task Send_adds_user_message_and_streams_assistant_reply()
    {
        var vm = new ChatViewModel(new FakeChatClient("При", "вет")) { Input = "привет" };

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Messages.Count);
        Assert.True(vm.Messages[0].IsUser);
        Assert.Equal("привет", vm.Messages[0].Content);
        Assert.False(vm.Messages[1].IsUser);
        Assert.Equal("Привет", vm.Messages[1].Content);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Send_does_nothing_when_input_blank()
    {
        var vm = new ChatViewModel(new FakeChatClient("x")) { Input = "   " };

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Empty(vm.Messages);
    }

    [Fact]
    public async Task Send_shows_error_text_when_request_fails()
    {
        var vm = new ChatViewModel(new ThrowingChatClient()) { Input = "hi" };

        await vm.SendCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Messages.Count);
        Assert.Contains("Не удалось", vm.Messages[1].Content);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void UseEndpoint_updates_target()
    {
        var vm = new ChatViewModel(new FakeChatClient());

        vm.UseEndpoint("http://127.0.0.1:9999/v1", "local/qwen", null);

        Assert.Equal("http://127.0.0.1:9999/v1", vm.BaseUrl);
        Assert.Equal("local/qwen", vm.Model);
    }
}
