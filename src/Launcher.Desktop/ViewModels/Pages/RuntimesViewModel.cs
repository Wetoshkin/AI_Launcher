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
using Launcher.Desktop.Services;
using Launcher.Runtimes.Hardware;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed record RuntimeReleaseRow(string Title, string Subtitle, RuntimeReleasePackage Package, bool IsRecommended = false);

public sealed record InstalledRuntimeRow(string Name, string FolderPath, string ExePath, string SizeText, bool IsActive);

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

    [ObservableProperty]
    private bool _hasReleases;

    [ObservableProperty]
    private bool _hasInstalled;

    [ObservableProperty]
    private string _installedStatus = string.Empty;

    public ObservableCollection<RuntimeReleaseRow> Releases { get; } = new();
    public ObservableCollection<InstalledRuntimeRow> Installed { get; } = new();

    public IReadOnlyList<RuntimeReleaseProfile> Profiles { get; } =
        new[] { RuntimeReleaseProfile.Vulkan, RuntimeReleaseProfile.Cuda, RuntimeReleaseProfile.Cpu, RuntimeReleaseProfile.Rocm };

    public string Title => "Среды (движок)";
    public string Description =>
        "«Движок» — это программа llama.cpp (её сервер llama-server), которая запускает GGUF-модель " +
        "на вашем ПК. Нужно один раз скачать готовую сборку под вашу видеокарту: CUDA — для NVIDIA, " +
        "Vulkan — для Intel/AMD, CPU — без видеокарты. llama.cpp работает в 2–3 раза быстрее Ollama при той же модели.";

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
        RefreshInstalled();
    }

    private static string RuntimeRoot => Path.Combine(AppDataRoot, "runtimes");

    /// <summary>Сканирует уже установленные движки и показывает их с возможностью удаления.</summary>
    [RelayCommand]
    private void RefreshInstalled()
    {
        Installed.Clear();
        var active = LocalServerLauncher.FindInstalledRuntime();

        try
        {
            if (!Directory.Exists(RuntimeRoot))
            {
                HasInstalled = false;
                InstalledStatus = "Пока ничего не установлено. Найдите и установите сборку ниже.";
                return;
            }

            foreach (var dir in Directory.EnumerateDirectories(RuntimeRoot))
            {
                var exe = Directory.EnumerateFiles(dir, "llama-server.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (exe is null)
                {
                    continue;
                }

                long size = 0;
                try
                {
                    size = Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                        .Sum(f => new FileInfo(f).Length);
                }
                catch
                {
                    // размер не критичен
                }

                Installed.Add(new InstalledRuntimeRow(
                    Path.GetFileName(dir),
                    dir,
                    exe,
                    $"{size / 1024.0 / 1024.0:0} МБ",
                    IsActive: string.Equals(exe, active, StringComparison.OrdinalIgnoreCase)));
            }

            HasInstalled = Installed.Count > 0;
            InstalledStatus = Installed.Count == 0
                ? "Пока ничего не установлено. Найдите и установите сборку ниже."
                : $"Установлено движков: {Installed.Count}. «✓ активный» — тот, что используется в Чате (самый свежий).";
        }
        catch (Exception ex)
        {
            InstalledStatus = "Не удалось прочитать список: " + ex.Message;
        }
    }

    [RelayCommand]
    private void DeleteRuntime(InstalledRuntimeRow? row)
    {
        if (row is null)
        {
            return;
        }

        try
        {
            Directory.Delete(row.FolderPath, recursive: true);
            InstalledStatus = $"Движок «{row.Name}» удалён.";
        }
        catch (Exception ex)
        {
            InstalledStatus = $"Не удалось удалить «{row.Name}»: {ex.Message}. " +
                "Возможно, сервер ещё запущен — остановите его в Чате и попробуйте снова.";
        }

        RefreshInstalled();
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
                if (shown >= 15)
                {
                    break;
                }

                var sizeMb = pkg.SizeBytes > 0 ? $"{pkg.SizeBytes / 1024.0 / 1024.0:0} МБ" : "размер неизв.";
                Releases.Add(new RuntimeReleaseRow(
                    pkg.AssetName,
                    $"{pkg.TagName} · {sizeMb}",
                    pkg,
                    IsRecommended: shown == 0));
                shown++;
            }

            HasReleases = Releases.Count > 0;
            Status = Releases.Count == 0
                ? "Под этот движок сборок не найдено. Попробуйте другой профиль."
                : "Это просто разные версии одного движка. Новичку проще всего нажать «Установить рекомендованную» — это самая свежая сборка (отмечена ⭐).";
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
    private Task InstallRecommendedAsync() =>
        Releases.Count > 0 ? DownloadAndInstallAsync(Releases[0]) : Task.CompletedTask;

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

                if (row.Package.CudartUrl is not null)
                {
                    Status = "Докачиваем библиотеки CUDA (нужны для NVIDIA)…";
                    await BundleCudartAsync(row.Package.CudartUrl, Path.GetDirectoryName(install.ExecutablePath!)!);
                }

                Status = $"Готово! Движок установлен: {install.ExecutablePath}. Перейдите в «Чат» → «Локальный сервер».";
                RefreshInstalled();
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

    /// <summary>Докачивает CUDA-библиотеки (cudart) и кладёт их DLL рядом с llama-server.exe.</summary>
    private static async Task BundleCudartAsync(Uri url, string installDir)
    {
        try
        {
            using var http = BuildDownloadHttp();
            var tmp = Path.Combine(Path.GetTempPath(), "cudart-" + Guid.NewGuid().ToString("N") + ".zip");
            var bytes = await http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(tmp, bytes);

            using (var zip = System.IO.Compression.ZipFile.OpenRead(tmp))
            {
                foreach (var entry in zip.Entries)
                {
                    if (entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        var dest = Path.Combine(installDir, entry.Name);
                        if (!File.Exists(dest))
                        {
                            await using var src = entry.Open();
                            await using var dst = File.Create(dest);
                            await src.CopyToAsync(dst);
                        }
                    }
                }
            }

            File.Delete(tmp);
        }
        catch
        {
            // докачка cudart — best-effort; без неё CUDA может не запуститься, но установка состоялась
        }
    }
}
