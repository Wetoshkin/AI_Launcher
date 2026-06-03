using System;
using System.Collections.Generic;
using System.Linq;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public static class RemoteDownloadQueueController
{
    public static string BuildStatusText(IReadOnlyCollection<RemoteDownloadQueueItemViewModel> queue)
    {
        if (queue.Count == 0)
        {
            return "Очередь HF пуста.";
        }

        var pending = queue.Count(item => item.Status == RemoteDownloadQueueItemStatus.Pending);
        var completed = queue.Count(item => item.Status == RemoteDownloadQueueItemStatus.Completed);
        var errors = queue.Count(item => item.Status == RemoteDownloadQueueItemStatus.Error);
        var downloading = queue.Count(item => item.Status == RemoteDownloadQueueItemStatus.Downloading);

        if (completed == 0 && errors == 0 && downloading == 0)
        {
            return $"В очереди HF: {pending} {PendingDownloadCountText(pending)}.";
        }

        return $"Очередь HF: скачивается {downloading}, завершено {completed}, ошибок {errors}, ожидают {pending}.";
    }

    public static RemoteDownloadQueueItemViewModel? FindExisting(
        IEnumerable<RemoteDownloadQueueItemViewModel> queue,
        string repoId,
        HuggingFaceGgufDownloadOption option) =>
        queue.FirstOrDefault(item => IsSameRemoteDownload(item.RepoId, item.Option, repoId, option));

    public static RemoteDownloadQueueRetryResult ResetFailedItems(IEnumerable<RemoteDownloadQueueItemViewModel> queue)
    {
        var failedItems = queue
            .Where(item => item.Status == RemoteDownloadQueueItemStatus.Error)
            .ToArray();

        foreach (var item in failedItems)
        {
            item.MarkPending();
        }

        return new RemoteDownloadQueueRetryResult(failedItems);
    }

    public static RemoteDownloadQueueClearResult ClearCompletedItems(
        ICollection<RemoteDownloadQueueItemViewModel> queue,
        RemoteDownloadQueueItemViewModel? selectedItem)
    {
        var completedItems = queue
            .Where(item => item.Status == RemoteDownloadQueueItemStatus.Completed)
            .ToArray();

        foreach (var item in completedItems)
        {
            queue.Remove(item);
        }

        var nextSelectedItem = selectedItem;
        if (nextSelectedItem is null || !queue.Contains(nextSelectedItem))
        {
            nextSelectedItem = queue.FirstOrDefault();
        }

        return new RemoteDownloadQueueClearResult(completedItems.Length, nextSelectedItem);
    }

    public static IReadOnlyList<RemoteDownloadQueueItemViewModel> ItemsToDownload(
        IEnumerable<RemoteDownloadQueueItemViewModel> queue) =>
        queue
            .Where(item => item.Status != RemoteDownloadQueueItemStatus.Completed)
            .ToArray();

    public static string PendingDownloadCountText(int count) => count % 10 == 1 && count % 100 != 11
        ? "ожидает скачивания"
        : "ожидают скачивания";

    public static string ErrorCountText(int count) => count % 10 == 1 && count % 100 != 11
        ? "ошибка"
        : "ошибок";

    private static bool IsSameRemoteDownload(
        string leftRepoId,
        HuggingFaceGgufDownloadOption leftOption,
        string rightRepoId,
        HuggingFaceGgufDownloadOption rightOption) =>
        string.Equals(leftRepoId, rightRepoId, StringComparison.OrdinalIgnoreCase)
        && string.Equals(leftOption.Label, rightOption.Label, StringComparison.OrdinalIgnoreCase)
        && leftOption.Files.Select(file => file.FileName)
            .SequenceEqual(rightOption.Files.Select(file => file.FileName), StringComparer.OrdinalIgnoreCase);
}

public sealed record RemoteDownloadQueueRetryResult(IReadOnlyList<RemoteDownloadQueueItemViewModel> Items)
{
    public int Count => Items.Count;
}

public sealed record RemoteDownloadQueueClearResult(int Count, RemoteDownloadQueueItemViewModel? SelectedItem);
