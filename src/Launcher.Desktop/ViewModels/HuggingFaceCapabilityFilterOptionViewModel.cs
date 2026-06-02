using Launcher.Models.HuggingFace;

namespace Launcher.Desktop.ViewModels;

public sealed class HuggingFaceCapabilityFilterOptionViewModel(HuggingFaceCapabilityFilter? filter, string label)
{
    public HuggingFaceCapabilityFilter? Filter => filter;

    public string Label => label;
}
