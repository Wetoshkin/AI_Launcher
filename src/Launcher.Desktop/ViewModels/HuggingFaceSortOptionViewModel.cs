using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public sealed class HuggingFaceSortOptionViewModel(HuggingFaceSort sort)
{
    public HuggingFaceSort Sort => sort;

    public string Label => sort switch
    {
        HuggingFaceSort.Downloads => "по загрузкам",
        HuggingFaceSort.Likes => "по лайкам",
        HuggingFaceSort.LastModified => "по дате обновления",
        HuggingFaceSort.Trending => "тренды",
        HuggingFaceSort.CreatedAt => "по дате создания",
        _ => sort.ToString()
    };
}
