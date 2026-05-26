namespace Launcher.Core.Decoding;

public static class DecodingPresetCatalog
{
    private static readonly IReadOnlyDictionary<string, DecodingPreset> Items =
        new Dictionary<string, DecodingPreset>(StringComparer.OrdinalIgnoreCase)
        {
            ["coding-safe"] = new(
                Id: "coding-safe",
                Name: "Безопасный coding",
                Description: "Обычный decoding без MTP и без --ignore-eos. Хороший режим по умолчанию для агентов.",
                EnableMtp: false,
                SpecType: null,
                IgnoreEos: false,
                LoopRisk: LoopRiskLevel.Low,
                Arguments: new Dictionary<string, string>
                {
                    ["--repeat-penalty"] = "1.08",
                    ["--presence-penalty"] = "0.0",
                    ["--frequency-penalty"] = "0.0"
                }),
            ["mtp-fast"] = new(
                Id: "mtp-fast",
                Name: "MTP быстрее",
                Description: "Speculative decoding через draft-mtp. Быстрее на совместимых runtime, но требует контроля повторов.",
                EnableMtp: true,
                SpecType: "draft-mtp",
                IgnoreEos: false,
                LoopRisk: LoopRiskLevel.Medium,
                Arguments: new Dictionary<string, string>
                {
                    ["--spec-type"] = "draft-mtp",
                    ["--spec-draft-n-max"] = "4",
                    ["--repeat-penalty"] = "1.08"
                })
        };

    public static IReadOnlyList<DecodingPreset> All => Items.Values.ToArray();

    public static DecodingPreset Get(string id)
    {
        if (Items.TryGetValue(id, out var preset))
        {
            return preset;
        }

        throw new KeyNotFoundException($"Unknown decoding preset: {id}");
    }
}
