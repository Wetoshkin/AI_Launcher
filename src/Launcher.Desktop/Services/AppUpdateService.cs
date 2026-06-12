using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.Services;

public sealed record AppUpdateResult(bool HasUpdate, string CurrentVersion, string? LatestVersion, string Message);

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

            return new AppUpdateResult(
                hasUpdate,
                AppInfo.Version,
                latestVersion,
                hasUpdate
                    ? $"Доступна новая версия {latestVersion}. Скачать: {AppInfo.ReleasesUrl}"
                    : "У вас последняя версия.");
        }
        catch (Exception ex)
        {
            return new AppUpdateResult(false, AppInfo.Version, null, "Не удалось проверить обновления: " + ex.Message);
        }
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
