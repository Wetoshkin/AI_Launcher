using System.Linq;
using Launcher.Online;

namespace Launcher.Online.Tests;

public class ProviderAndFactoryTests
{
    [Fact]
    public void Registry_has_known_providers()
    {
        var kinds = ProviderRegistry.All.Select(p => p.Kind).ToList();
        Assert.Contains(ProviderKind.Local, kinds);
        Assert.Contains(ProviderKind.OpenAi, kinds);
        Assert.Contains(ProviderKind.OpenRouter, kinds);
        Assert.Contains(ProviderKind.Anthropic, kinds);
    }

    [Fact]
    public void Anthropic_preset_is_marked_anthropic_and_requires_key()
    {
        var anthropic = ProviderRegistry.ForKind(ProviderKind.Anthropic);
        Assert.True(anthropic.IsAnthropic);
        Assert.True(anthropic.RequiresKey);
    }

    [Fact]
    public void Local_preset_does_not_require_key()
    {
        Assert.False(ProviderRegistry.ForKind(ProviderKind.Local).RequiresKey);
    }

    [Fact]
    public void Factory_picks_anthropic_client_for_anthropic_provider()
    {
        var client = ChatClientFactory.Create(ProviderRegistry.ForKind(ProviderKind.Anthropic));
        Assert.IsType<AnthropicChatClient>(client);
    }

    [Fact]
    public void Factory_picks_openai_client_for_compatible_provider()
    {
        var client = ChatClientFactory.Create(ProviderRegistry.ForKind(ProviderKind.OpenAi));
        Assert.IsType<OpenAiChatClient>(client);
    }
}
