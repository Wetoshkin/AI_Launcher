namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceModelDownloadRequest(
    string RepoId,
    HuggingFaceGgufDownloadOption Option,
    string ModelsDirectory);
