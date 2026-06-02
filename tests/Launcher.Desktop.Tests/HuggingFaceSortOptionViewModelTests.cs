using Launcher.Desktop.ViewModels;
using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.Tests;

public sealed class HuggingFaceSortOptionViewModelTests
{
    [Theory]
    [InlineData(HuggingFaceSort.Downloads, "по загрузкам")]
    [InlineData(HuggingFaceSort.Likes, "по лайкам")]
    [InlineData(HuggingFaceSort.LastModified, "по дате обновления")]
    [InlineData(HuggingFaceSort.Trending, "тренды")]
    [InlineData(HuggingFaceSort.CreatedAt, "по дате создания")]
    public void LabelUsesRussianTextAndKeepsSortValue(HuggingFaceSort sort, string expectedLabel)
    {
        var option = new HuggingFaceSortOptionViewModel(sort);

        Assert.Equal(sort, option.Sort);
        Assert.Equal(expectedLabel, option.Label);
    }
}
