using System.Collections.ObjectModel;
using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.Tests;

public sealed class RemoteDownloadQueueControllerTests
{
    [Fact]
    public void BuildStatusTextPreservesEmptyAndPendingRussianText()
    {
        Assert.Equal("Очередь HF пуста.", RemoteDownloadQueueController.BuildStatusText([]));

        var queue = new[]
        {
            CreateItem("unsloth/Pending-GGUF", "Pending-Q4_K_M.gguf")
        };

        Assert.Equal("В очереди HF: 1 ожидает скачивания.", RemoteDownloadQueueController.BuildStatusText(queue));
    }

    [Fact]
    public void BuildStatusTextCountsAllQueueStates()
    {
        var pending = CreateItem("unsloth/Pending-GGUF", "Pending-Q4_K_M.gguf");
        var downloading = CreateItem("unsloth/Downloading-GGUF", "Downloading-Q4_K_M.gguf");
        var completed = CreateItem("unsloth/Completed-GGUF", "Completed-Q4_K_M.gguf");
        var error = CreateItem("unsloth/Error-GGUF", "Error-Q4_K_M.gguf");
        downloading.MarkDownloading();
        completed.MarkCompleted();
        error.MarkError();

        var text = RemoteDownloadQueueController.BuildStatusText([pending, downloading, completed, error]);

        Assert.Equal("Очередь HF: скачивается 1, завершено 1, ошибок 1, ожидают 1.", text);
    }

    [Fact]
    public void ResetFailedItemsMarksOnlyErrorsPending()
    {
        var completed = CreateItem("unsloth/Completed-GGUF", "Completed-Q4_K_M.gguf");
        var error = CreateItem("unsloth/Error-GGUF", "Error-Q4_K_M.gguf");
        completed.MarkCompleted();
        error.MarkError();

        var result = RemoteDownloadQueueController.ResetFailedItems([completed, error]);

        var failed = Assert.Single(result.Items);
        Assert.Same(error, failed);
        Assert.Equal(1, result.Count);
        Assert.Equal("ожидает скачивания", error.StatusText);
        Assert.Equal("завершено", completed.StatusText);
    }

    [Fact]
    public void ClearCompletedItemsRemovesOnlyCompletedAndKeepsValidSelection()
    {
        var queue = new ObservableCollection<RemoteDownloadQueueItemViewModel>
        {
            CreateItem("unsloth/Pending-GGUF", "Pending-Q4_K_M.gguf"),
            CreateItem("unsloth/Completed-GGUF", "Completed-Q4_K_M.gguf"),
            CreateItem("unsloth/Error-GGUF", "Error-Q4_K_M.gguf")
        };
        queue[1].MarkCompleted();
        queue[2].MarkError();

        var result = RemoteDownloadQueueController.ClearCompletedItems(queue, queue[1]);

        Assert.Equal(1, result.Count);
        Assert.DoesNotContain(queue, item => item.Label == "Completed-Q4_K_M.gguf");
        Assert.Same(queue[0], result.SelectedItem);
    }

    private static RemoteDownloadQueueItemViewModel CreateItem(string repoId, string fileName) =>
        new(repoId, new HuggingFaceGgufDownloadOption(
            fileName,
            "Q4_K_M",
            IsSplit: false,
            [new HuggingFaceGgufFile(fileName, $"https://example.test/{fileName}", IsFirstSplitShard: false)]));
}
