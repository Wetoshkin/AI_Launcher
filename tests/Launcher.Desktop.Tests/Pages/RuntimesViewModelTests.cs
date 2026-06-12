using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Desktop.ViewModels.Pages;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.Tests.Pages;

public class RuntimesViewModelTests
{
    private sealed class FakeCatalog : IRuntimeReleaseCatalog
    {
        public Task<IReadOnlyList<RuntimeReleasePackage>> ListPackagesAsync(
            RuntimeReleaseProfile profile, CancellationToken cancellationToken)
        {
            IReadOnlyList<RuntimeReleasePackage> packages = new[]
            {
                new RuntimeReleasePackage("b9999", "release", DateTimeOffset.UtcNow,
                    "llama-b9999-bin-win-vulkan-x64.zip", new Uri("https://example.com/a.zip"),
                    123_000_000, false, RuntimeReleaseAssetSource.Stable),
            };
            return Task.FromResult(packages);
        }
    }

    [Fact]
    public async Task List_releases_populates_rows()
    {
        var vm = new RuntimesViewModel(new FakeCatalog());

        await vm.ListReleasesCommand.ExecuteAsync(null);

        Assert.Single(vm.Releases);
        Assert.Contains("vulkan", vm.Releases[0].Title);
        Assert.Contains("b9999", vm.Releases[0].Subtitle);
    }

    [Fact]
    public void Apply_hardware_recommends_vulkan_for_intel_arc()
    {
        var vm = new RuntimesViewModel(new FakeCatalog());
        var hw = new SystemHardware("Ultra 7", new[] { new GpuInfo("Intel Arc", 0, 2.0) }, 31.5, 16.0);

        vm.ApplyHardware(hw);

        Assert.Equal(RuntimeReleaseProfile.Vulkan, vm.SelectedProfile);
        Assert.Contains("Vulkan", vm.Recommendation);
    }

    [Fact]
    public void Apply_hardware_recommends_cuda_for_nvidia()
    {
        var vm = new RuntimesViewModel(new FakeCatalog());
        var hw = new SystemHardware("Ryzen", new[] { new GpuInfo("NVIDIA GeForce RTX 4090", 0, 24.0) }, 64.0, 40.0);

        vm.ApplyHardware(hw);

        Assert.Equal(RuntimeReleaseProfile.Cuda, vm.SelectedProfile);
    }
}
