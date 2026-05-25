using Launcher.Core.Migration;

namespace Launcher.Core.Tests;

public sealed class PythonLauncherConfigImporterTests
{
    [Fact]
    public void ImportsKnownFoldersWithoutMutatingSourceConfig()
    {
        var json = """
        {
          "models_dir": "D:\\AI\\Models",
          "projects_dir": "D:\\AI\\Projects",
          "llama_server_path": "D:\\AI\\runtimes\\llama-server.exe"
        }
        """;

        var result = PythonLauncherConfigImporter.Import(json);

        Assert.Equal(@"D:\AI\Models", result.ModelsRoot);
        Assert.Equal(@"D:\AI\Projects", result.ProjectsRoot);
        Assert.Equal(@"D:\AI\runtimes", result.RuntimeRoot);
    }
}
