using Launcher.Core.Profiles;
using Launcher.Core.Scenarios;
using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;
using Launcher.Runtimes.Ports;
using Launcher.Runtimes.Processes;
using Launcher.Runtimes.Startup;
using Launcher.Runtimes.Status;

namespace Launcher.Desktop.Tests;

public sealed class PresetViewModelTests
{
    [Fact]
    public void PresetRowShowsUsefulRussianSummary()
    {
        var preset = new PresetRowViewModel(new LaunchProfile(
            "kilo-qwen",
            "Kilo + Qwen Coder",
            LaunchMode.Agent,
            AgentKind.Kilo,
            RuntimeKind.LlamaCppTurboQuant,
            @"D:\AI\Projects\App",
            @"D:\AI\Models\qwen.gguf",
            65536,
            8080,
            "coding-safe"));

        Assert.Equal("Kilo + Qwen Coder", preset.Name);
        Assert.Equal("Проект · Kilo · LlamaCppTurboQuant · 64k · порт 8080", preset.Summary);
        Assert.Equal(@"D:\AI\Projects\App", preset.ProjectPath);
    }

    [Fact]
    public void ApplySelectedPresetUpdatesDraftLaunchState()
    {
        var viewModel = new HomeViewModel(
            new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
            new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector()),
            new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
            new EmptyDownloadService());
        var preset = new PresetRowViewModel(new LaunchProfile(
            "opencode-gemma",
            "OpenCode + Gemma",
            LaunchMode.Agent,
            AgentKind.OpenCode,
            RuntimeKind.Ollama,
            @"D:\AI\Projects\App",
            @"D:\AI\Models\gemma.gguf",
            32768,
            11434,
            "coding-safe"));

        viewModel.Presets.Clear();
        viewModel.Presets.Add(preset);
        viewModel.SelectedPreset = preset;
        viewModel.ApplySelectedPresetCommand.Execute(null);

        Assert.Equal(AgentKind.OpenCode, viewModel.SelectedAgent);
        Assert.Equal(RuntimeKind.Ollama, viewModel.SelectedRuntime);
        Assert.Equal(@"D:\AI\Projects\App", viewModel.ProjectsFolderPath);
        Assert.Equal(11434, viewModel.Port);
        Assert.Equal(32768, viewModel.ContextTokens);
        Assert.Equal("Пресет применён: OpenCode + Gemma.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task LoadSettingsCommandReplacesDefaultPresetsWithStoredProfiles()
    {
        var store = new MemorySettingsStore(new LauncherSettings(
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
                    "saved-kilo",
                    "Сохранённый Kilo",
                    LaunchMode.Agent,
                    AgentKind.Kilo,
                    RuntimeKind.LlamaCppTurboQuant,
                    @"D:\AI\Projects\Saved",
                    @"D:\AI\Models\saved.gguf",
                    65536,
                    8080,
                    "coding-safe")
            ]));
        var viewModel = CreateViewModel(store);

        await viewModel.LoadSettingsCommand.ExecuteAsync(null);

        var preset = Assert.Single(viewModel.Presets);
        Assert.Equal("Сохранённый Kilo", preset.Name);
        Assert.Equal(@"D:\AI\Models", viewModel.ModelsFolderPath);
        Assert.Equal(@"D:\AI\Projects", viewModel.ProjectsFolderPath);
        Assert.Equal(@"D:\AI\runtimes", viewModel.RuntimeRootPath);
        Assert.Equal(@"D:\AI\downloads", viewModel.RuntimeCacheRootPath);
    }

    [Fact]
    public async Task SaveCurrentPresetCommandPersistsDraftProfile()
    {
        var store = new MemorySettingsStore(null);
        var viewModel = CreateViewModel(store);
        viewModel.SelectedAgent = AgentKind.Claw;
        viewModel.SelectedRuntime = RuntimeKind.LlamaCppMtp;
        viewModel.Port = 8081;
        viewModel.ContextTokens = 131072;
        viewModel.RuntimeRootPath = @"D:\AI\runtimes-custom";
        viewModel.RuntimeCacheRootPath = @"D:\AI\runtime-cache-custom";

        await viewModel.SaveCurrentPresetCommand.ExecuteAsync(null);

        Assert.NotNull(store.Saved);
        Assert.Equal(@"D:\AI\runtimes-custom", store.Saved.RuntimeRoot);
        Assert.Equal(@"D:\AI\runtime-cache-custom", store.Saved.DownloadsRoot);
        Assert.Equal(4, store.Saved.Profiles.Count);
        var profile = store.Saved.Profiles.Last();
        Assert.Equal("Быстрый запуск 4", profile.Name);
        Assert.Equal(AgentKind.Claw, profile.Agent);
        Assert.Equal(RuntimeKind.LlamaCppMtp, profile.Runtime);
        Assert.Equal(8081, profile.Port);
        Assert.Equal(131072, profile.ContextTokens);
        Assert.Equal("Пресет сохранён: Быстрый запуск 4.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task SaveCurrentPresetCommandPersistsDetectedRuntimeVersionSource()
    {
        var store = new MemorySettingsStore(null);
        var executable = @"D:\AI\runtimes\b5300\llama-server.exe";
        var viewModel = CreateViewModel(
            settingsStore: store,
            runtimeCatalog: new FakeRuntimeCatalog([Runtime(executable)]));

        await viewModel.CheckPortCommand.ExecuteAsync(null);
        await viewModel.SaveCurrentPresetCommand.ExecuteAsync(null);

        Assert.NotNull(store.Saved);
        Assert.Equal(executable, store.Saved.LastRuntimeVersionSource);
    }

    [Fact]
    public async Task InstallRuntimePackageCommandInstallsArchiveIntoRuntimeRoot()
    {
        var installer = new CapturingRuntimeInstaller(new RuntimePackageInstallResult(
            Installed: true,
            InstallDirectory: @"D:\AI\runtimes\llama-runtime",
            ExecutablePath: @"D:\AI\runtimes\llama-runtime\llama-server.exe",
            Message: "llama-server.exe найден"));
        var viewModel = CreateViewModel(runtimePackageInstaller: installer);
        viewModel.RuntimeArchivePath = @"D:\Downloads\llama-runtime.zip";
        viewModel.RuntimeRootPath = @"D:\AI\runtimes";

        await viewModel.InstallRuntimePackageCommand.ExecuteAsync(null);

        Assert.NotNull(installer.LastRequest);
        Assert.Equal(@"D:\Downloads\llama-runtime.zip", installer.LastRequest.ArchivePath);
        Assert.Equal(@"D:\AI\runtimes", installer.LastRequest.RuntimeRoot);
        Assert.Equal("llama-runtime", installer.LastRequest.RuntimeId);
        Assert.Equal("runtime: llama-server.exe найден", viewModel.RuntimeStatus);
        Assert.Equal("Runtime установлен: llama-server.exe найден", viewModel.StatusMessage);
    }

    [Fact]
    public async Task InstalledRuntimeIsUsedForLaunchCommandWithoutExtraScan()
    {
        using var temp = new TempDirectory();
        var executable = @"D:\AI\runtimes\llama-runtime\llama-server.exe";
        var installer = new CapturingRuntimeInstaller(new RuntimePackageInstallResult(
            Installed: true,
            InstallDirectory: @"D:\AI\runtimes\llama-runtime",
            ExecutablePath: executable,
            Message: "llama-server.exe найден"));
        var viewModel = CreateViewModel(runtimePackageInstaller: installer);
        viewModel.RuntimeArchivePath = @"D:\Downloads\llama-runtime.zip";
        viewModel.RuntimeRootPath = @"D:\AI\runtimes";
        viewModel.FolderPicker = new FixedFolderPicker(temp.Path);
        viewModel.SelectEndpointModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);

        await viewModel.InstallRuntimePackageCommand.ExecuteAsync(null);
        viewModel.BuildLaunchCommandCommand.Execute(null);

        Assert.Contains(executable, viewModel.LaunchCommandPreview);
    }

    [Fact]
    public async Task AgentLaunchCommandUsesSelectedGgufNameAsProviderModel()
    {
        using var temp = new TempDirectory();
        var viewModel = CreateViewModel();
        viewModel.FolderPicker = new FixedFolderPicker(temp.Path);
        viewModel.SelectAgentModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);

        viewModel.BuildLaunchCommandCommand.Execute(null);

        Assert.Contains("local/Qwen3-Coder-Q4_K_M", viewModel.LaunchCommandPreview);
        Assert.DoesNotContain("local/llama.cpp/model", viewModel.LaunchCommandPreview);
    }

    [Fact]
    public async Task AgentLaunchCommandPreviewShowsServerAndAgentStages()
    {
        using var temp = new TempDirectory();
        var viewModel = CreateViewModel();
        viewModel.FolderPicker = new FixedFolderPicker(temp.Path);
        viewModel.SelectAgentModeCommand.Execute(null);
        await viewModel.ChooseModelsFolderCommand.ExecuteAsync(null);

        viewModel.BuildLaunchCommandCommand.Execute(null);

        Assert.Contains("SERVER:", viewModel.LaunchCommandPreview);
        Assert.Contains("AGENT:", viewModel.LaunchCommandPreview);
        Assert.Contains("--alias local/Qwen3-Coder-Q4_K_M", viewModel.LaunchCommandPreview);
        Assert.Contains("kilo -m local/Qwen3-Coder-Q4_K_M", viewModel.LaunchCommandPreview);
    }

    [Fact]
    public void ClearProcessLogCommandRemovesExistingLogLines()
    {
        var viewModel = CreateViewModel();
        viewModel.ProcessLogLines.Add("server started");
        viewModel.ProcessLogLines.Add("agent started");

        viewModel.ClearProcessLogCommand.Execute(null);

        Assert.Empty(viewModel.ProcessLogLines);
        Assert.Equal("Лог очищен.", viewModel.StatusMessage);
    }

    [Fact]
    public void ClearProcessLogCommandKeepsLastLaunchLogSnapshot()
    {
        var viewModel = CreateViewModel();
        viewModel.ProcessLogLines.Add("server started");
        viewModel.ProcessLogLines.Add("agent started");

        viewModel.ClearProcessLogCommand.Execute(null);

        Assert.Equal(["server started", "agent started"], viewModel.LastLaunchLogLines);
    }

    [Fact]
    public async Task PickRuntimeArchiveCommandStoresSelectedZipPath()
    {
        var picker = new FixedFilePicker(@"D:\Downloads\llama-runtime.zip");
        var viewModel = CreateViewModel();
        viewModel.FilePicker = picker;

        await viewModel.PickRuntimeArchiveCommand.ExecuteAsync(null);

        Assert.Equal(@"D:\Downloads\llama-runtime.zip", viewModel.RuntimeArchivePath);
        Assert.Equal("Runtime-архив выбран.", viewModel.StatusMessage);
        Assert.Equal(".zip", Assert.Single(picker.LastExtensions));
    }

    [Fact]
    public async Task PickRuntimeArchiveCommandKeepsExistingPathWhenCancelled()
    {
        var picker = new FixedFilePicker(null);
        var viewModel = CreateViewModel();
        viewModel.RuntimeArchivePath = @"D:\Downloads\old-runtime.zip";
        viewModel.FilePicker = picker;

        await viewModel.PickRuntimeArchiveCommand.ExecuteAsync(null);

        Assert.Equal(@"D:\Downloads\old-runtime.zip", viewModel.RuntimeArchivePath);
        Assert.Equal("Выбор runtime-архива отменён.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ChooseRuntimeRootFolderCommandUpdatesRuntimeRootPath()
    {
        var viewModel = CreateViewModel();
        viewModel.FolderPicker = new FixedFolderPicker(@"D:\AI\runtimes-new");

        await viewModel.ChooseRuntimeRootFolderCommand.ExecuteAsync(null);

        Assert.Equal(@"D:\AI\runtimes-new", viewModel.RuntimeRootPath);
        Assert.Equal("Папка установки runtime обновлена.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task ChooseRuntimeCacheFolderCommandUpdatesRuntimeCacheRootPath()
    {
        var viewModel = CreateViewModel();
        viewModel.FolderPicker = new FixedFolderPicker(@"D:\AI\runtime-cache");

        await viewModel.ChooseRuntimeCacheFolderCommand.ExecuteAsync(null);

        Assert.Equal(@"D:\AI\runtime-cache", viewModel.RuntimeCacheRootPath);
        Assert.Equal("Папка кэша runtime обновлена.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task SearchRuntimeReleasesCommandDisplaysSelectablePackages()
    {
        var package = RuntimePackage("b5400", "llama-b5400-bin-win-cuda-x64.zip", 128_000_000);
        var catalog = new FakeRuntimeReleaseCatalog([package]);
        var viewModel = CreateViewModel(runtimeReleaseCatalog: catalog);
        viewModel.SelectedRuntimeReleaseProfile = RuntimeReleaseProfile.Cuda;

        await viewModel.SearchRuntimeReleasesCommand.ExecuteAsync(null);

        Assert.Equal(RuntimeReleaseProfile.Cuda, catalog.LastProfile);
        var row = Assert.Single(viewModel.RuntimeReleasePackages);
        Assert.Equal(package, row.Package);
        Assert.Same(row, viewModel.SelectedRuntimeReleasePackage);
        Assert.Equal("Найдено runtime-пакетов: 1.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task SearchRuntimeReleasesCommandFiltersPackagesBySelectedSource()
    {
        var stable = RuntimePackage(
            "b5400",
            "llama-b5400-bin-win-cuda-x64.zip",
            128_000_000,
            RuntimeReleaseAssetSource.Stable);
        var latest = RuntimePackage(
            "b5410",
            "llama-b5410-bin-win-cuda-x64.zip",
            129_000_000,
            RuntimeReleaseAssetSource.Latest);
        var viewModel = CreateViewModel(runtimeReleaseCatalog: new FakeRuntimeReleaseCatalog([stable, latest]));
        viewModel.SelectedRuntimeReleaseSource = RuntimeReleaseAssetSource.Latest;

        await viewModel.SearchRuntimeReleasesCommand.ExecuteAsync(null);

        var row = Assert.Single(viewModel.RuntimeReleasePackages);
        Assert.Equal(latest, row.Package);
        Assert.Equal("последний релиз", row.SourceLabel);
        Assert.Same(row, viewModel.SelectedRuntimeReleasePackage);
    }

    [Fact]
    public void RuntimeReleasePackageRowShowsRussianSourceLabel()
    {
        var package = RuntimePackage(
            "b5410",
            "llama-b5410-bin-win-cuda-x64.zip",
            129_000_000,
            RuntimeReleaseAssetSource.Latest);

        var row = new RuntimeReleasePackageRowViewModel(package);

        Assert.Equal("последний релиз", row.SourceLabel);
        Assert.DoesNotContain("последний релиз", row.Summary);
    }

    [Fact]
    public async Task CheckRuntimeUpdateCommandReportsAvailableUpdate()
    {
        var package = RuntimePackage("b5400", "llama-b5400-bin-win-cuda-x64.zip", 128_000_000);
        var viewModel = CreateViewModel(runtimeReleaseCatalog: new FakeRuntimeReleaseCatalog([package]));
        viewModel.RuntimeArchivePath = @"D:\AI\cache\b5300\llama-b5300-bin-win-cuda-x64.zip";
        viewModel.SelectedRuntimeReleaseProfile = RuntimeReleaseProfile.Cuda;

        await viewModel.CheckRuntimeUpdateCommand.ExecuteAsync(null);

        Assert.Equal("доступно обновление: b5300 -> b5400", viewModel.RuntimeUpdateStatus);
        Assert.Equal("Проверка обновления runtime: доступно обновление: b5300 -> b5400", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CheckRuntimeUpdateCommandUsesSelectedSource()
    {
        var stable = RuntimePackage(
            "b5400",
            "llama-b5400-bin-win-cuda-x64.zip",
            128_000_000,
            RuntimeReleaseAssetSource.Stable);
        var latest = RuntimePackage(
            "b5410",
            "llama-b5410-bin-win-cuda-x64.zip",
            129_000_000,
            RuntimeReleaseAssetSource.Latest);
        var viewModel = CreateViewModel(runtimeReleaseCatalog: new FakeRuntimeReleaseCatalog([stable, latest]));
        viewModel.RuntimeArchivePath = @"D:\AI\cache\b5300\llama-b5300-bin-win-cuda-x64.zip";
        viewModel.SelectedRuntimeReleaseSource = RuntimeReleaseAssetSource.Latest;

        await viewModel.CheckRuntimeUpdateCommand.ExecuteAsync(null);

        Assert.Equal("доступно обновление: b5300 -> b5410", viewModel.RuntimeUpdateStatus);
    }

    [Fact]
    public async Task CheckRuntimeUpdateCommandUsesDetectedRuntimeWhenArchivePathIsEmpty()
    {
        var package = RuntimePackage("b5400", "llama-b5400-bin-win-cuda-x64.zip", 128_000_000);
        var viewModel = CreateViewModel(
            runtimeReleaseCatalog: new FakeRuntimeReleaseCatalog([package]),
            runtimeCatalog: new FakeRuntimeCatalog([Runtime(@"D:\AI\runtimes\b5300\llama-server.exe")]));
        viewModel.RuntimeArchivePath = "";

        await viewModel.CheckPortCommand.ExecuteAsync(null);
        await viewModel.CheckRuntimeUpdateCommand.ExecuteAsync(null);

        Assert.Equal("доступно обновление: b5300 -> b5400", viewModel.RuntimeUpdateStatus);
    }

    [Fact]
    public async Task CheckRuntimeUpdateCommandUsesSavedRuntimeVersionSourceAfterSettingsLoad()
    {
        var package = RuntimePackage("b5400", "llama-b5400-bin-win-cuda-x64.zip", 128_000_000);
        var store = new MemorySettingsStore(new LauncherSettings(
            ModelsRoot: @"D:\AI\Models",
            ProjectsRoot: @"D:\AI\Projects",
            RuntimeRoot: @"D:\AI\runtimes",
            DownloadsRoot: @"D:\AI\downloads",
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "pro",
            Profiles: [])
        {
            LastRuntimeVersionSource = @"D:\AI\runtimes\b5300\llama-server.exe"
        });
        var viewModel = CreateViewModel(
            settingsStore: store,
            runtimeReleaseCatalog: new FakeRuntimeReleaseCatalog([package]));
        viewModel.RuntimeArchivePath = "";

        await viewModel.LoadSettingsCommand.ExecuteAsync(null);
        await viewModel.CheckRuntimeUpdateCommand.ExecuteAsync(null);

        Assert.Equal("доступно обновление: b5300 -> b5400", viewModel.RuntimeUpdateStatus);
    }

    [Fact]
    public async Task LoadSettingsCommandShowsSavedRuntimeVersionSource()
    {
        var store = new MemorySettingsStore(new LauncherSettings(
            ModelsRoot: @"D:\AI\Models",
            ProjectsRoot: @"D:\AI\Projects",
            RuntimeRoot: @"D:\AI\runtimes",
            DownloadsRoot: @"D:\AI\downloads",
            DefaultPort: 8080,
            Language: "ru",
            HelpMode: "pro",
            Profiles: [])
        {
            LastRuntimeVersionSource = @"D:\AI\runtimes\b5300\llama-server.exe"
        });
        var viewModel = CreateViewModel(settingsStore: store);

        await viewModel.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal(@"Источник версии runtime: D:\AI\runtimes\b5300\llama-server.exe", viewModel.RuntimeVersionSourceText);
    }

    [Fact]
    public void SelectedRuntimeReleaseProfileUpdatesRussianHint()
    {
        var viewModel = CreateViewModel();

        viewModel.SelectedRuntimeReleaseProfile = RuntimeReleaseProfile.Cuda;

        Assert.Equal("CUDA: для видеокарт NVIDIA, обычно самый быстрый вариант для RTX.", viewModel.RuntimeReleaseProfileHint);
    }

    [Fact]
    public void RuntimeReleaseProfileOptionsExposeRussianLabelsAndTooltips()
    {
        var viewModel = CreateViewModel();

        var labels = viewModel.RuntimeReleaseProfileOptions
            .Select(option => option!.GetType().GetProperty("Label")?.GetValue(option)?.ToString())
            .ToArray();
        var tooltips = viewModel.RuntimeReleaseProfileOptions
            .Select(option => option!.GetType().GetProperty("Tooltip")?.GetValue(option)?.ToString())
            .ToArray();

        Assert.Equal(["Процессор", "NVIDIA CUDA", "Vulkan для видеокарт", "AMD ROCm"], labels);
        Assert.Equal(
            [
                "Самый совместимый вариант без ускорения видеокартой.",
                "Для видеокарт NVIDIA, обычно лучший выбор для RTX.",
                "Универсальный вариант для видеокарт NVIDIA, AMD и Intel.",
                "Для совместимых видеокарт AMD Radeon и Instinct."
            ],
            tooltips);
        Assert.DoesNotContain(labels, label => string.Equals(label, RuntimeReleaseProfile.Cpu.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(labels, label => string.Equals(label, RuntimeReleaseProfile.Cuda.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(labels, label => string.Equals(label, RuntimeReleaseProfile.Rocm.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public void HuggingFaceSortOptionsExposeRussianLabelsAndKeepSortValues()
    {
        var viewModel = CreateViewModel();

        var labels = viewModel.HfSortOptions
            .Select(option => option!.GetType().GetProperty("Label")?.GetValue(option)?.ToString())
            .ToArray();
        var sorts = viewModel.HfSortOptions
            .Select(option => option!.GetType().GetProperty("Sort")?.GetValue(option))
            .ToArray();

        Assert.Equal(["по загрузкам", "по лайкам", "по дате обновления", "тренды"], labels);
        Assert.Equal(
            [HuggingFaceSort.Downloads, HuggingFaceSort.Likes, HuggingFaceSort.LastModified, HuggingFaceSort.Trending],
            sorts);
        Assert.DoesNotContain(labels, label => string.Equals(label, HuggingFaceSort.Downloads.ToString(), StringComparison.Ordinal));
        Assert.DoesNotContain(labels, label => string.Equals(label, HuggingFaceSort.LastModified.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReleasePortCommandStopsSafeLlamaServerOwner()
    {
        var owner = new PortOwnerInfo(8080, 4242, "llama-server", @"D:\AI\runtimes\llama-server.exe", false, null);
        var releaser = new RecordingPortReleaser(new PortReleaseResult(true, "Остановлен llama-server на порту 8080."));
        var viewModel = CreateViewModel(
            portInspector: new FixedPortInspector(owner),
            portReleaser: releaser);

        await viewModel.ReleasePortCommand.ExecuteAsync(null);

        Assert.Same(owner, releaser.ReleasedOwner);
        Assert.Equal("порт 8080: освобождён", viewModel.PortStatus);
        Assert.Equal("Остановлен llama-server на порту 8080.", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DownloadSelectedRuntimeReleaseCommandStoresDownloadedArchivePath()
    {
        var package = RuntimePackage("b5400", "llama-b5400-bin-win-cuda-x64.zip", 128_000_000);
        var downloader = new FakeRuntimeReleaseDownloader(
            new RuntimeReleaseDownloadResult(@"D:\AI\cache\b5400\llama.zip", Downloaded: true, Skipped: false, "архив скачан"),
            new RuntimeReleaseDownloadProgress("llama.zip", 64, 128, IsSkipped: false));
        var viewModel = CreateViewModel(runtimeReleaseDownloader: downloader);
        var row = new RuntimeReleasePackageRowViewModel(package);
        viewModel.RuntimeReleasePackages.Add(row);
        viewModel.SelectedRuntimeReleasePackage = row;
        viewModel.RuntimeCacheRootPath = @"D:\AI\cache";

        await viewModel.DownloadSelectedRuntimeReleaseCommand.ExecuteAsync(null);

        Assert.NotNull(downloader.LastRequest);
        Assert.Equal(package, downloader.LastRequest.Package);
        Assert.Equal(@"D:\AI\cache", downloader.LastRequest.CacheRoot);
        Assert.Equal(@"D:\AI\cache\b5400\llama.zip", viewModel.RuntimeArchivePath);
        Assert.Equal("Runtime: llama.zip · 50%", viewModel.RuntimeDownloadProgressText);
        Assert.Equal("Runtime скачан: архив скачан", viewModel.StatusMessage);
    }

    [Fact]
    public async Task DownloadAndInstallSelectedRuntimeReleaseCommandInstallsDownloadedArchive()
    {
        var package = RuntimePackage("b5400", "llama-b5400-bin-win-cuda-x64.zip", 128_000_000);
        var downloader = new FakeRuntimeReleaseDownloader(
            new RuntimeReleaseDownloadResult(@"D:\AI\cache\b5400\llama.zip", Downloaded: true, Skipped: false, "архив скачан"));
        var installer = new CapturingRuntimeInstaller(new RuntimePackageInstallResult(
            Installed: true,
            InstallDirectory: @"D:\AI\runtimes\llama",
            ExecutablePath: @"D:\AI\runtimes\llama\llama-server.exe",
            Message: "llama-server.exe найден"));
        var viewModel = CreateViewModel(
            runtimePackageInstaller: installer,
            runtimeReleaseDownloader: downloader);
        var row = new RuntimeReleasePackageRowViewModel(package);
        viewModel.RuntimeReleasePackages.Add(row);
        viewModel.SelectedRuntimeReleasePackage = row;
        viewModel.RuntimeCacheRootPath = @"D:\AI\cache";
        viewModel.RuntimeRootPath = @"D:\AI\runtimes";

        await viewModel.DownloadAndInstallSelectedRuntimeReleaseCommand.ExecuteAsync(null);

        Assert.NotNull(installer.LastRequest);
        Assert.Equal(@"D:\AI\cache\b5400\llama.zip", installer.LastRequest.ArchivePath);
        Assert.Equal(@"D:\AI\runtimes", installer.LastRequest.RuntimeRoot);
        Assert.Equal("llama", installer.LastRequest.RuntimeId);
        Assert.Equal("runtime: llama-server.exe найден", viewModel.RuntimeStatus);
        Assert.Equal("Runtime скачан и установлен: llama-server.exe найден", viewModel.StatusMessage);
    }

    [Fact]
    public async Task CancelRuntimeDownloadCommandCancelsActiveDownload()
    {
        var package = RuntimePackage("b5400", "llama-b5400-bin-win-cuda-x64.zip", 128_000_000);
        var downloader = new BlockingRuntimeReleaseDownloader();
        var viewModel = CreateViewModel(runtimeReleaseDownloader: downloader);
        var row = new RuntimeReleasePackageRowViewModel(package);
        viewModel.RuntimeReleasePackages.Add(row);
        viewModel.SelectedRuntimeReleasePackage = row;

        var downloadTask = viewModel.DownloadSelectedRuntimeReleaseCommand.ExecuteAsync(null);
        await downloader.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(viewModel.IsRuntimeDownloading);
        viewModel.CancelRuntimeDownloadCommand.Execute(null);
        await downloadTask;

        Assert.True(downloader.WasCanceled);
        Assert.False(viewModel.IsRuntimeDownloading);
        Assert.Equal("Скачивание runtime отменено.", viewModel.StatusMessage);
    }

    [Fact]
    public void AgentCliStatusRowShowsRussianMissingPath()
    {
        var row = new AgentCliStatusRowViewModel(new Launcher.Agents.Discovery.AgentCliStatus(
            AgentKind.Kilo,
            "kilo",
            IsInstalled: false,
            ExecutablePath: null,
            VersionText: null));

        Assert.Equal("Kilo", row.Name);
        Assert.Equal("kilo", row.Executable);
        Assert.Equal("не найден", row.Status);
        Assert.Equal("не найден в PATH", row.Path);
        Assert.False(row.IsInstalled);
    }

    private static HomeViewModel CreateViewModel(
        ILauncherSettingsStore? settingsStore = null,
        IRuntimePackageInstaller? runtimePackageInstaller = null,
        IRuntimeReleaseCatalog? runtimeReleaseCatalog = null,
        IRuntimeReleaseDownloader? runtimeReleaseDownloader = null,
        IPortInspector? portInspector = null,
        IPortReleaser? portReleaser = null,
        ILlamaRuntimeCatalog? runtimeCatalog = null) => new(
        new HuggingFaceModelClient(new HttpClient(new EmptyHttpHandler()) { BaseAddress = new Uri("https://huggingface.co") }),
        new RuntimeDashboardService(new EmptyGpuProbe(), new EmptyPortInspector(), runtimeCatalog),
        new RuntimeStartCoordinator(new EmptyPortInspector(), new EmptyPortReleaser(), new EmptyProcessStarter()),
        new EmptyDownloadService(),
        settingsStore,
        agentCliCatalogService: null,
        runtimePackageInstaller: runtimePackageInstaller ?? new EmptyRuntimeInstaller(),
        runtimeReleaseCatalog: runtimeReleaseCatalog,
        runtimeReleaseDownloader: runtimeReleaseDownloader,
        portInspector: portInspector,
        portReleaser: portReleaser);

    private static RuntimeReleasePackage RuntimePackage(
        string tag,
        string assetName,
        long sizeBytes,
        RuntimeReleaseAssetSource source = RuntimeReleaseAssetSource.Stable) => new(
        tag,
        $"Release {tag}",
        PublishedAt: new DateTimeOffset(2026, 5, 28, 0, 0, 0, TimeSpan.Zero),
        assetName,
        new Uri("https://github.com/runtime.zip"),
        sizeBytes,
        Prerelease: false,
        source);

    private static LlamaRuntimeInfo Runtime(string executablePath) => new(
        executablePath,
        new LlamaServerCapabilities(
            new HashSet<string>(),
            new HashSet<string>(),
            new HashSet<string>(),
            SupportsTurboQuant: true,
            SupportsMtp: false));

    private sealed class EmptyDownloadService : IHuggingFaceModelDownloadService
    {
        public Task<HuggingFaceModelDownloadResult> DownloadAsync(
            HuggingFaceModelDownloadRequest request,
            CancellationToken cancellationToken,
            Action<HuggingFaceDownloadProgress>? progress = null) =>
            Task.FromResult(new HuggingFaceModelDownloadResult([], []));
    }

    private sealed class EmptyRuntimeInstaller : IRuntimePackageInstaller
    {
        public Task<RuntimePackageInstallResult> InstallAsync(RuntimePackageInstallRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new RuntimePackageInstallResult(false, request.RuntimeRoot, null, "не используется"));
    }

    private sealed class CapturingRuntimeInstaller(RuntimePackageInstallResult result) : IRuntimePackageInstaller
    {
        public RuntimePackageInstallRequest? LastRequest { get; private set; }

        public Task<RuntimePackageInstallResult> InstallAsync(RuntimePackageInstallRequest request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeRuntimeReleaseCatalog(IReadOnlyList<RuntimeReleasePackage> packages) : IRuntimeReleaseCatalog
    {
        public RuntimeReleaseProfile? LastProfile { get; private set; }

        public Task<IReadOnlyList<RuntimeReleasePackage>> ListPackagesAsync(
            RuntimeReleaseProfile profile,
            CancellationToken cancellationToken)
        {
            LastProfile = profile;
            return Task.FromResult(packages);
        }
    }

    private sealed class FakeRuntimeCatalog(IReadOnlyList<LlamaRuntimeInfo> runtimes) : ILlamaRuntimeCatalog
    {
        public Task<IReadOnlyList<LlamaRuntimeInfo>> ScanAsync(IEnumerable<string> runtimeRoots, CancellationToken cancellationToken) =>
            Task.FromResult(runtimes);
    }

    private sealed class FakeRuntimeReleaseDownloader(
        RuntimeReleaseDownloadResult result,
        RuntimeReleaseDownloadProgress? progress = null) : IRuntimeReleaseDownloader
    {
        public RuntimeReleaseDownloadRequest? LastRequest { get; private set; }

        public Task<RuntimeReleaseDownloadResult> DownloadAsync(
            RuntimeReleaseDownloadRequest request,
            CancellationToken cancellationToken,
            Action<RuntimeReleaseDownloadProgress>? progressCallback = null)
        {
            LastRequest = request;
            if (progress is not null)
            {
                progressCallback?.Invoke(progress);
            }

            return Task.FromResult(result);
        }
    }

    private sealed class BlockingRuntimeReleaseDownloader : IRuntimeReleaseDownloader
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WasCanceled { get; private set; }

        public async Task<RuntimeReleaseDownloadResult> DownloadAsync(
            RuntimeReleaseDownloadRequest request,
            CancellationToken cancellationToken,
            Action<RuntimeReleaseDownloadProgress>? progressCallback = null)
        {
            Started.SetResult();
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                WasCanceled = true;
                throw;
            }

            throw new InvalidOperationException("Test downloader should be canceled.");
        }
    }

    private sealed class FixedFilePicker(string? path) : Launcher.Desktop.Services.IFilePicker
    {
        public IReadOnlyList<string> LastExtensions { get; private set; } = [];

        public Task<string?> PickFileAsync(string title, IReadOnlyList<string> extensions)
        {
            LastExtensions = extensions;
            return Task.FromResult(path);
        }
    }

    private sealed class FixedFolderPicker(string path) : Launcher.Desktop.Services.IFolderPicker
    {
        public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(path);
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "preset-vm-" + Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
            using var file = File.Create(System.IO.Path.Combine(Path, "Qwen3-Coder-Q4_K_M.gguf"));
            file.SetLength(128L * 1024L * 1024L);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class MemorySettingsStore(LauncherSettings? initial) : ILauncherSettingsStore
    {
        public LauncherSettings? Saved { get; private set; } = initial;

        public Task<LauncherSettings?> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Saved);

        public Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken)
        {
            Saved = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
    }

    private sealed class EmptyGpuProbe : IGpuProbe
    {
        public Task<IReadOnlyList<GpuInfo>> GetGpusAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GpuInfo>>([]);
    }

    private sealed class EmptyPortInspector : IPortInspector
    {
        public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken) =>
            Task.FromResult<PortOwnerInfo?>(null);
    }

    private sealed class FixedPortInspector(PortOwnerInfo? owner) : IPortInspector
    {
        public Task<PortOwnerInfo?> InspectAsync(int port, CancellationToken cancellationToken) =>
            Task.FromResult(owner);
    }

    private sealed class EmptyPortReleaser : IPortReleaser
    {
        public Task<PortReleaseResult> ReleaseIfSafeAsync(PortOwnerInfo owner, CancellationToken cancellationToken) =>
            Task.FromResult(new PortReleaseResult(Released: false, "не требуется"));
    }

    private sealed class RecordingPortReleaser(PortReleaseResult result) : IPortReleaser
    {
        public PortOwnerInfo? ReleasedOwner { get; private set; }

        public Task<PortReleaseResult> ReleaseIfSafeAsync(PortOwnerInfo owner, CancellationToken cancellationToken)
        {
            ReleasedOwner = owner;
            return Task.FromResult(result);
        }
    }

    private sealed class EmptyProcessStarter : IProcessStarter
    {
        public Task<ProcessStartResult> StartAsync(ProcessStartRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new ProcessStartResult(0));
    }
}
