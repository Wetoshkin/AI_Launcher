using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.Tests;

public sealed class RemoteModelRowViewModelTests
{
    [Fact]
    public void ConstructorExposesDownloadOptionsForConcreteGgufFiles()
    {
        var model = new HuggingFaceModelSummary(
            "unsloth/Qwen3-Coder-GGUF",
            Downloads: 1234,
            Likes: 56,
            Tags: ["gguf", "qwen"],
            IsCompatibleWithCurrentGpu: false,
            HasPreferredQuant: true,
            IsRuntimeCompatible: true,
            SiblingFiles:
            [
                "Qwen3-Coder-Q4_K_M.gguf",
                "Qwen3-Coder-Q5_K_M-00001-of-00002.gguf",
                "Qwen3-Coder-Q5_K_M-00002-of-00002.gguf"
            ]);

        var row = new RemoteModelRowViewModel(model);

        Assert.Equal("2 GGUF", row.DownloadOptionsText);
        Assert.Collection(row.DownloadOptions,
            option =>
            {
                Assert.Equal("Qwen3-Coder-Q4_K_M.gguf", option.Label);
                Assert.Equal("Q4_K_M", option.Quant);
                Assert.Equal("1 файл", option.FileCountText);
            },
            option =>
            {
                Assert.Equal("Qwen3-Coder-Q5_K_M.gguf", option.Label);
                Assert.Equal("Q5_K_M", option.Quant);
                Assert.Equal("2 файла", option.FileCountText);
            });
    }
}
