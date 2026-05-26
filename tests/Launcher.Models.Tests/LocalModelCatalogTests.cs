using Launcher.Models.Catalog;

namespace Launcher.Models.Tests;

public sealed class LocalModelCatalogTests
{
    [Fact]
    public void ScansGgufFilesAndSkipsMmprojTinyFilesAndLaterSplitShards()
    {
        var root = CreateTempModelRoot();
        try
        {
            WriteSparseFile(Path.Combine(root, "Qwen3-Coder-30B-Q4_K_M.gguf"), 129);
            WriteSparseFile(Path.Combine(root, "mmproj-Qwen.gguf"), 129);
            WriteSparseFile(Path.Combine(root, "tiny.gguf"), 1);
            WriteSparseFile(Path.Combine(root, "Gemma-27B-Q8_0-00001-of-00002.gguf"), 129);
            WriteSparseFile(Path.Combine(root, "Gemma-27B-Q8_0-00002-of-00002.gguf"), 129);

            var models = LocalModelCatalog.Scan([root]);

            Assert.Equal(2, models.Count);
            Assert.Contains(models, model => Path.GetFileName(model.Path) == "Qwen3-Coder-30B-Q4_K_M.gguf");
            Assert.Contains(models, model => Path.GetFileName(model.Path) == "Gemma-27B-Q8_0-00001-of-00002.gguf");
            Assert.DoesNotContain(models, model => Path.GetFileName(model.Path).Contains("00002-of"));
            Assert.DoesNotContain(models, model => Path.GetFileName(model.Path).Contains("mmproj", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempModelRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"launcher-models-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteSparseFile(string path, int sizeMb)
    {
        using var stream = File.Create(path);
        stream.SetLength(sizeMb * 1024L * 1024L);
    }
}
