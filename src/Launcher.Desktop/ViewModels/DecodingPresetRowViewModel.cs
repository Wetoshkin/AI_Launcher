using Launcher.Core.Decoding;

namespace Launcher.Desktop.ViewModels;

public sealed class DecodingPresetRowViewModel(DecodingPreset preset)
{
    public string Id => preset.Id;

    public string Name => preset.Name;

    public string Description => preset.Description;

    public string Risk => preset.LoopRisk switch
    {
        LoopRiskLevel.Low => "низкий риск",
        LoopRiskLevel.Medium => "средний риск",
        LoopRiskLevel.High => "высокий риск",
        _ => preset.LoopRisk.ToString()
    };
}
