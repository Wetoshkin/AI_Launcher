namespace Launcher.Models.HuggingFace;

public sealed record HuggingFaceModelSearchRequest(
    string Query,
    HuggingFaceSort Sort,
    int Limit = 20,
    bool GgufOnly = true);
