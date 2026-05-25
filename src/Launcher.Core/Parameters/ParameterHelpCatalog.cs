namespace Launcher.Core.Parameters;

public static class ParameterHelpCatalog
{
    private static readonly IReadOnlyDictionary<string, ParameterHelp> Items =
        new Dictionary<string, ParameterHelp>(StringComparer.OrdinalIgnoreCase)
        {
            ["context"] = new("context", "Контекст",
                "Сколько токенов модель держит в памяти.",
                "Больше контекст дает больше рабочей памяти для проекта, но увеличивает расход VRAM/RAM.",
                ParameterRiskLevel.Normal),
            ["mtp"] = new("mtp", "MTP",
                "Ускорение через предсказание нескольких токенов вперед.",
                "MTP может ускорить генерацию на совместимых моделях и runtime-ах, но при агрессивных настройках повышает риск повторов.",
                ParameterRiskLevel.Warning),
            ["ignore-eos"] = new("ignore-eos", "--ignore-eos",
                "Опасно: модель может не остановиться сама.",
                "Используйте только для диагностики. В agent workflow этот флаг может усиливать зацикливание.",
                ParameterRiskLevel.Danger)
        };

    public static ParameterHelp Get(string id)
    {
        if (Items.TryGetValue(id, out var help)) return help;
        throw new KeyNotFoundException($"Unknown parameter help id: {id}");
    }
}
