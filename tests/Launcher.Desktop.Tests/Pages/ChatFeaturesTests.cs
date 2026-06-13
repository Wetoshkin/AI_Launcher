using System.Linq;
using Launcher.Desktop.ViewModels.Pages;

namespace Launcher.Desktop.Tests.Pages;

public class ChatFeaturesTests
{
    [Theory]
    [InlineData("Qwen3-30B-A3B-Q4_K_M.gguf", true)]
    [InlineData("Mixtral-8x7B-Instruct-Q4_K_M.gguf", true)]
    [InlineData("glm-4.6-q4_k_m.gguf", true)]
    [InlineData("qwen2.5-coder-7b-instruct-q4_k_m.gguf", false)]
    [InlineData("llama-3.1-8b-q4_k_m.gguf", false)]
    public void Detects_moe_models_by_name(string file, bool expected)
    {
        Assert.Equal(expected, AgentsViewModel.IsLikelyMoE(file));
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
