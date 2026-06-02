using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public sealed class RemoteDownloadQueueItemViewModel(string repoId, HuggingFaceGgufDownloadOption option) : ViewModelBase
{
    private RemoteDownloadQueueItemStatus _status = RemoteDownloadQueueItemStatus.Pending;

    public string RepoId => repoId;

    public HuggingFaceGgufDownloadOption Option => option;

    public string Label => option.Label;

    internal RemoteDownloadQueueItemStatus Status => _status;

    public string StatusText => _status switch
    {
        RemoteDownloadQueueItemStatus.Downloading => "скачивается",
        RemoteDownloadQueueItemStatus.Completed => "завершено",
        RemoteDownloadQueueItemStatus.Error => "ошибка",
        _ => "ожидает скачивания"
    };

    public void MarkPending() => SetStatus(RemoteDownloadQueueItemStatus.Pending);

    public void MarkDownloading() => SetStatus(RemoteDownloadQueueItemStatus.Downloading);

    public void MarkCompleted() => SetStatus(RemoteDownloadQueueItemStatus.Completed);

    public void MarkError() => SetStatus(RemoteDownloadQueueItemStatus.Error);

    private void SetStatus(RemoteDownloadQueueItemStatus status)
    {
        if (_status == status)
        {
            return;
        }

        _status = status;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusText));
    }
}

internal enum RemoteDownloadQueueItemStatus
{
    Pending,
    Downloading,
    Completed,
    Error
}
