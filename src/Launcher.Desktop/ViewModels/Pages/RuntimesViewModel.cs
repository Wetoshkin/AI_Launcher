using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed record RuntimeReleaseRow(string Title, string Subtitle, RuntimeReleasePackage Package);

public sealed partial class RuntimesViewModel : ViewModelBase
{
    private readonly IRuntimeReleaseCatalog _catalog;
    private readonly IRuntimeReleaseDownloader _downloader;
    private readonly IRuntimePackageInstaller _installer;

    [ObservableProperty]
    private RuntimeReleaseProfile _selectedProfile = RuntimeReleaseProfile.Vulkan;

    [ObservableProperty]
    private string _recommendation =
        "Для Intel/AMD выбирайте Vulkan, для NVIDIA — CUDA, без видеокарты — CPU.";

    [ObservableProperty]
    private string _status = "Выберите движок под ваше железо и нажмите «Найти сборки».";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _runtimePath = string.Empty;

    public ObservableCollection<RuntimeReleaseRow> Releases { get; } = new();

    public IReadOnlyList<RuntimeReleaseProfile> Profiles { get; } =
        new[] { RuntimeReleaseProfile.Vulkan, RuntimeReleaseProfile.Cuda, RuntimeReleaseProfile.Cpu, RuntimeReleaseProfile.Rocm };

    public string Title => "Среды (runtime)";
    public string Description =>
        "Движок llama.cpp выполняет модель. Скачайте сборку под ваше железо. " +
        "llama.cpp работает в 2–3 раза быстрее Ollama при той же модели.";

    public RuntimesViewModel()
        : this(BuildDefaultCatalog(), new RuntimeReleaseDownloadService(BuildDownloadHttp()), new RuntimePackageInstaller())
    {
    }

    public RuntimesViewModel(IRuntimeReleaseCatalog catalog)
        : this(catalog, new RuntimeReleaseDownloadService(BuildDownloadHttp()), new RuntimePackageInstaller())
    {
    }

    public RuntimesViewModel(
        IRuntimeReleaseCatalog catalog,
        IRuntimeReleaseDownloader downloader,
        IRuntimePackageInstaller installer)
    {
        _catalog = catalog;
        _downloader = downloader;
        _installer = installer;
    }

    private static IRuntimeReleaseCatalog BuildDefaultCatalog()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Launcher-Studio/0.1");
        return new RuntimeReleaseCatalogService(new GitHubReleaseClient(http), "ggml-org", "llama.cpp");
    }

    private static HttpClient BuildDownloadHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Launcher-Studio/0.1");
        return http;
    }

    private static string AppDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AILauncherStudio");

    /// <summary>Подбирает рекомендованный движок под обнаруженное железо.</summary>
    public void ApplyHardware(SystemHardware hardware)
    {
        var hasNvidia = hardware.Gpus.Any(g => g.Name.Contains("nvidia", StringComparison.OrdinalIgnoreCase)
                                               || g.Name.Contains("geforce", StringComparison.OrdinalIgnoreCase)
                                               || g.Name.Contains("rtx", StringComparison.OrdinalIgnoreCase));
        if (hasNvidia)
        {
            SelectedProfile = RuntimeReleaseProfile.Cuda;
            Recommendation = "У вас видеокарта NVIDIA — рекомендуем сборку CUDA (самая быстрая).";
        }
        else if (hardware.HasGpu)
        {
            SelectedProfile = RuntimeReleaseProfile.Vulkan;
            Recommendation = $"У вас {hardware.Gpus[0].Name} — рекомендуем сборку Vulkan (работает на Intel/AMD).";
        }
        else
        {
            SelectedProfile = RuntimeReleaseProfile.Cpu;
            Recommendation = "Видеокарта не найдена — рекомендуем сборку CPU.";
        }
    }

    private bool CanList => !IsBusy;

    partial void OnIsBusyChanged(bool value) => ListReleasesCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanList))]
    private async Task ListReleasesAsync()
    {
        IsBusy = true;
        Status = "Запрашиваем сборки llama.cpp с GitHub…";
        Releases.Clear();

        try
        {
            var packages = await _catalog.ListPackagesAsync(SelectedProfile, CancellationToken.None);
            var shown = 0;
            foreach (var pkg in packages)
            {
                if (shown++ >= 15)
                {
                    break;
                }

                var sizeMb = pkg.SizeBytes > 0 ? $"{pkg.SizeBytes / 1024.0 / 1024.0:0} МБ" : "размер неизв.";
                Releases.Add(new RuntimeReleaseRow(
                    pkg.AssetName,
                    $"{pkg.TagName} · {sizeMb}",
                    pkg));
            }

            Status = Releases.Count == 0
                ? "Под этот движок сборок не найдено. Попробуйте другой профиль."
                : $"Найдено сборок: {Releases.Count}. Нажмите «Скачать и установить» у подходящей.";
        }
        catch (Exception ex)
        {
            Status = "Не удалось получить список: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAndInstallAsync(RuntimeReleaseRow? row)
    {
        if (row is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        Status = $"Скачиваем {row.Package.AssetName}…";

        try
        {
            var cacheRoot = Path.Combine(AppDataRoot, "cache");
            var runtimeRoot = Path.Combine(AppDataRoot, "runtimes");

            var download = await _downloader.DownloadAsync(
                new RuntimeReleaseDownloadRequest(row.Package, cacheRoot),
                CancellationToken.None);

            Status = "Устанавливаем…";
            var runtimeId = $"{SelectedProfile}-{row.Package.TagName}".Replace('/', '-');
            var install = await _installer.InstallAsync(
                new RuntimePackageInstallRequest(download.ArchivePath, runtimeRoot, runtimeId),
                CancellationToken.None);

            if (install.Installed && !string.IsNullOrWhiteSpace(install.ExecutablePath))
            {
                RuntimePath = install.ExecutablePath!;
                Status = $"Готово! llama-server установлен: {install.ExecutablePath}";
            }
            else
            {
                Status = "Установка не удалась: " + install.Message;
            }
        }
        catch (Exception ex)
        {
            Status = "Ошибка скачивания/установки: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
