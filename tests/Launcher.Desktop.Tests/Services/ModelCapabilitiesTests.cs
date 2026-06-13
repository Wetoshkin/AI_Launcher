using Launcher.Desktop.Services;
using Xunit;

namespace Launcher.Desktop.Tests.Services;

public class ModelCapabilitiesTests
{
    [Fact]
    public void Detects_tools_from_chat_template()
    {
        var caps = ModelCapabilityDetector.Detect("qwen2.5-7b-instruct-q4_k_m.gguf",
            chatTemplate: "{% if tools %}...{{ tool_call }}...", hasMmproj: false);
        Assert.True(caps.Tools);
    }

    [Fact]
    public void Detects_vision_from_mmproj()
    {
        var caps = ModelCapabilityDetector.Detect("model-q4_k_m.gguf", chatTemplate: null, hasMmproj: true);
        Assert.True(caps.Vision);
    }

    [Fact]
    public void Detects_vision_from_name()
    {
        var caps = ModelCapabilityDetector.Detect("Qwen2.5-VL-7B-Instruct-Q4_K_M.gguf", chatTemplate: null, hasMmproj: false);
        Assert.True(caps.Vision);
    }

    [Fact]
    public void Detects_reasoning_from_name()
    {
        var caps = ModelCapabilityDetector.Detect("DeepSeek-R1-Distill-Qwen-7B-Q4_K_M.gguf", chatTemplate: null, hasMmproj: false);
        Assert.True(caps.Reasoning);
    }

    [Fact]
    public void Plain_model_has_no_caps()
    {
        var caps = ModelCapabilityDetector.Detect("llama-3.1-8b-q4_k_m.gguf", chatTemplate: "{{ messages }}", hasMmproj: false);
        Assert.False(caps.Any);
    }
}
