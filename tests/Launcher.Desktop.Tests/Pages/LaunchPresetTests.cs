using System.Linq;
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests.Pages;

public class LaunchPresetTests
{
    [Fact]
    public void Has_three_presets_ordered_by_context()
    {
        var presets = LaunchPreset.All;
        Assert.Equal(3, presets.Count);
        Assert.Equal(new[] { 2048, 4096, 8192 }, presets.Select(p => p.ContextTokens).ToArray());
    }

    [Fact]
    public void Default_is_balanced()
    {
        Assert.Equal("Сбалансированно", LaunchPreset.Default.Name);
        Assert.Equal(4096, LaunchPreset.Default.ContextTokens);
    }

    [Fact]
    public void Chat_uses_balanced_preset_by_default()
    {
        var vm = new ChatViewModel();
        Assert.Equal(4096, vm.SelectedPreset.ContextTokens);
    }
}
