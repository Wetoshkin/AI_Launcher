using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.Services;

public sealed record AppUpdateResult(
    bool HasUpdate,
    string CurrentVersion,
    string? LatestVersion,
    string Message,
    Uri? InstallerUrl = null);

/// <summary>Проверяет наличие новой версии приложения по релизам GitHub-репозитория.</summary>
public sealed class AppUpdateService
{
    private readonly GitHubReleaseClient _client;

    public AppUpdateService(GitHubReleaseClient? client = null)
    {
        if (client is null)
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Launcher-Studio/" + AppInfo.Version);
            client = new GitHubReleaseClient(http);
        }

        _client = client;
    }

    public async Task<AppUpdateResult> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            var releases = await _client.ListAsync(AppInfo.RepoOwner, AppInfo.RepoName, cancellationToken);
            var latest = releases
                .Where(r => !r.Draft && !r.Prerelease && !string.IsNullOrWhiteSpace(r.TagName))
                .OrderByDescending(r => r.PublishedAt)
                .FirstOrDefault();

            if (latest is null)
            {
                return new AppUpdateResult(false, AppInfo.Version, null, "Релизов пока нет.");
            }

            var latestVersion = latest.TagName.TrimStart('v', 'V');
            var hasUpdate = CompareVersions(latestVersion, AppInfo.Version) > 0;

            var installer = latest.Assets.FirstOrDefault(a =>
                a.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase)
                && a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

            return new AppUpdateResult(
                hasUpdate,
                AppInfo.Version,
                latestVersion,
                hasUpdate
                    ? (installer is not null
                        ? $"Доступна новая версия {latestVersion}. Можно обновиться прямо сейчас."
                        : $"Доступна новая версия {latestVersion}. Скачать: {AppInfo.ReleasesUrl}")
                    : "У вас последняя версия.",
                hasUpdate ? installer?.DownloadUrl : null);
        }
        catch (Exception ex)
        {
            return new AppUpdateResult(false, AppInfo.Version, null, "Не удалось проверить обновления: " + ex.Message);
        }
    }

    /// <summary>Скачивает установщик во временную папку, сообщая прогресс (0..1).</summary>
    public async Task<string> DownloadInstallerAsync(
        Uri url,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AI-Launcher-Studio/" + AppInfo.Version);

        var fileName = Path.GetFileName(url.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            fileName = "AI-Launcher-Studio-Setup.exe";
        }

        var dest = Path.Combine(Path.GetTempPath(), fileName);

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength;

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var target = File.Create(dest))
        {
            var buffer = new byte[81920];
            long received = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;
                if (total is > 0)
                {
                    progress?.Report((double)received / total.Value);
                }
            }
        }

        return dest;
    }

    /// <summary>
    /// Запускает установщик в тихом режиме: он закроет это приложение, обновит файлы и снова запустит его.
    /// </summary>
    public static void LaunchInstaller(string installerPath)
    {
        var psi = new ProcessStartInfo(installerPath)
        {
            UseShellExecute = true,
        };
        // Inno Setup: тихая установка с прогрессом, закрыть и перезапустить приложение.
        psi.ArgumentList.Add("/SILENT");
        psi.ArgumentList.Add("/CLOSEAPPLICATIONS");
        psi.ArgumentList.Add("/RESTARTAPPLICATIONS");
        psi.ArgumentList.Add("/NOICONS");
        Process.Start(psi);
    }

    /// <summary>Сравнивает версии вида 1.2.3. &gt;0 если a новее b.</summary>
    public static int CompareVersions(string a, string b)
    {
        var pa = Parse(a);
        var pb = Parse(b);
        for (var i = 0; i < 3; i++)
        {
            if (pa[i] != pb[i])
            {
                return pa[i].CompareTo(pb[i]);
            }
        }

        return 0;
    }

    private static int[] Parse(string version)
    {
        var parts = version.Split('.', '-');
        var result = new int[3];
        for (var i = 0; i < 3 && i < parts.Length; i++)
        {
            int.TryParse(parts[i], out result[i]);
        }

        return result;
    }
}
