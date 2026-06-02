using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;

namespace Launcher.Core.Tests;

public sealed class LauncherSettingsFileStoreTests
{
    [Fact]
    public async Task SaveAndLoadAsyncRoundTripsSettingsAndCreatesDirectory()
    {
        using var temp = new TempDirectory();
        var filePath = Path.Combine(temp.Path, "nested", "launcher-settings.json");
        var store = new LauncherSettingsFileStore(filePath);
        var settings = new LauncherSettings(
            ModelsRoot: @"D:\AI\Models",
            ProjectsRoot: @"D:\AI\Projects",
            RuntimeRoot: @"D:\AI\runtimes",
            DownloadsRoot: @"D:\AI\downloads",
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "pro",
            Profiles:
            [
                new LaunchProfile(
                    "endpoint-mtp",
                    "Сервер MTP",
                    LaunchMode.Endpoint,
                    AgentKind.None,
                    RuntimeKind.LlamaCppMtp,
                    ProjectPath: null,
                    ModelPath: @"D:\AI\Models\hermes.gguf",
                    ContextTokens: 65536,
                    Port: 8081,
                    AntiLoopPresetId: "mtp-fast")
                {
                    KvCache = new KvCacheSettings(
                        TypeK: "q8_0",
                        TypeV: "q6_k",
                        FlashAttention: true,
                        OffloadKqv: false),
                    Mtp = new MtpSettings(
                        Enabled: true,
                        DraftModelPath: @"D:\AI\Models\hermes-draft.gguf",
                        DraftTokens: 4,
                        SpeculativeType: "mtp")
                }
            ])
        {
            HuggingFaceFilters = new HuggingFaceFilterSettings(
                SearchQuery: "hermes coder",
                Author: "NousResearch",
                Quantization: "Q4_K_M",
                Architecture: "llama",
                Task: "text-generation",
                Sort: "downloads",
                ShowGated: false,
                ShowIncompatible: true)
        };

        await store.SaveAsync(settings, CancellationToken.None);
        var restored = await store.LoadAsync(CancellationToken.None);

        Assert.True(File.Exists(filePath));
        Assert.NotNull(restored);
        Assert.Equal(settings.ModelsRoot, restored.ModelsRoot);
        Assert.Equal(settings.ProjectsRoot, restored.ProjectsRoot);
        Assert.Equal(settings.RuntimeRoot, restored.RuntimeRoot);
        Assert.Equal(settings.DownloadsRoot, restored.DownloadsRoot);
        Assert.Equal(settings.DefaultPort, restored.DefaultPort);
        Assert.Equal(settings.Language, restored.Language);
        Assert.Equal(settings.HelpMode, restored.HelpMode);
        Assert.Equal(settings.Profiles, restored.Profiles);
        Assert.Equal(settings.HuggingFaceFilters, restored.HuggingFaceFilters);
    }

    [Fact]
    public async Task LoadAsyncReturnsNullWhenFileDoesNotExist()
    {
        using var temp = new TempDirectory();
        var store = new LauncherSettingsFileStore(Path.Combine(temp.Path, "missing.json"));

        var restored = await store.LoadAsync(CancellationToken.None);

        Assert.Null(restored);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "launcher-settings-" + Guid.NewGuid().ToString("N"));

        public TempDirectory() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
