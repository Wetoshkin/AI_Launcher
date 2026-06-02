using System;
using Launcher.Runtimes.LlamaCpp;

namespace Launcher.Desktop.ViewModels;

public sealed class RuntimeReleasePackageRowViewModel(RuntimeReleasePackage package)
{
    public RuntimeReleasePackage Package => package;

    public string Title => $"{package.TagName} · {package.AssetName}";

    public string Summary => $"{SizeText(package.SizeBytes)} · {DateText(package.PublishedAt)}";

    public string SourceLabel => package.SourceLabel;

    public string Url => package.DownloadUrl.ToString();

    private static string SizeText(long sizeBytes) =>
        sizeBytes > 0
            ? $"{sizeBytes / 1024d / 1024d:0.0} МБ"
            : "размер неизвестен";

    private static string DateText(DateTimeOffset? publishedAt) =>
        publishedAt is { } date
            ? date.ToString("yyyy-MM-dd")
            : "дата неизвестна";
}
