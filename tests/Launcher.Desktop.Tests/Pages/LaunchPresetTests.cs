using System.Linq;
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests.Pages;

public class LaunchPresetTests
{
    [Fact]
    public void Presets_are_ordered_by_context()
    {
        var presets = LaunchPreset.All;
        Assert.Equal(new[] { 8192, 16384, 32768, 65536, 131072 }, presets.Select(p => p.ContextTokens).ToArray());
    }

    [Fact]
    public void Default_is_32k_for_agents()
    {
        Assert.Equal(32768, LaunchPreset.Default.ContextTokens);
    }

    [Fact]
    public void Agents_use_32k_preset_by_default()
    {
        var vm = new AgentsViewModel();
        Assert.Equal(32768, vm.SelectedPreset.ContextTokens);
    }
}
