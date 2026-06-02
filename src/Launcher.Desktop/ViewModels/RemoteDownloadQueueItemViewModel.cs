using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public sealed class RemoteDownloadQueueItemViewModel(string repoId, HuggingFaceGgufDownloadOption option)
{
    public string RepoId => repoId;

    public HuggingFaceGgufDownloadOption Option => option;

    public string Label => option.Label;

    public string StatusText => "ожидает скачивания";
}
