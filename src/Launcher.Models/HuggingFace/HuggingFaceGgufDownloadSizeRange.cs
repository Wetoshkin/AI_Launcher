namespace Launcher.Models.HuggingFace;

public enum HuggingFaceGgufDownloadSizeRange
{
    Any,
    UpTo4Gb,
    UpTo8Gb,
    Between8And16Gb,
    UpTo16Gb,
    Between16And32Gb,
    Over16Gb,
    Over32Gb,
    Unknown
}
