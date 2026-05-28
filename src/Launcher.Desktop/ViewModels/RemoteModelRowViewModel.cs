using System.Collections.Generic;
using System.Linq;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public sealed class RemoteModelRowViewModel(HuggingFaceModelSummary model)
{
    public HuggingFaceModelSummary Model => model;

    public string Id => model.Id;

    public string Downloads => $"{model.Downloads:N0}";

    public string Likes => $"{model.Likes:N0}";

    public string Tags => string.Join(", ", model.Tags.Take(4));

    public string Compatibility => model.IsRuntimeCompatible
        ? "GGUF"
        : "требует проверки";

    public IReadOnlyList<RemoteGgufDownloadOptionRowViewModel> DownloadOptions { get; } =
        HuggingFaceGgufFileSelector.SelectDownloadOptions(model)
            .Select(option => new RemoteGgufDownloadOptionRowViewModel(model.Id, option))
            .ToArray();

    public string DownloadOptionsText => $"{DownloadOptions.Count} GGUF";
}
