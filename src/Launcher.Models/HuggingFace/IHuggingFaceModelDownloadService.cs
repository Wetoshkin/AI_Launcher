namespace Launcher.Models.HuggingFace;

public interface IHuggingFaceModelDownloadService
{
    Task<HuggingFaceModelDownloadResult> DownloadAsync(
        HuggingFaceModelDownloadRequest request,
        CancellationToken cancellationToken,
        Action<HuggingFaceDownloadProgress>? progress = null);
}
