using System;

namespace Launcher.Desktop.Services;

/// <summary>
/// Простая, понятная новичку оценка «влезет ли модель в железо».
/// Level: 0 — быстро (целиком в видеопамять), 1 — медленнее (часть в ОЗУ), 2 — не влезет, 3 — размер неизвестен.
/// </summary>
public static class ModelFit
{
    public static (string Text, int Level) Describe(double sizeGb, double vramGb, double ramGb)
    {
        if (sizeGb <= 0)
        {
            return ("Размер неизвестен", 3);
        }

        // ~1.5 ГБ запас под KV-кэш и контекст в видеопамяти.
        if (vramGb > 0 && sizeGb + 1.5 <= vramGb)
        {
            return ($"Влезет в видеопамять — быстро · {sizeGb:0.0} ГБ", 0);
        }

        // Часть слоёв уйдёт в ОЗУ (offload) — работать будет, но медленнее.
        if (sizeGb + 3 <= ramGb + Math.Max(0, vramGb))
        {
            return ($"Влезет, часть в ОЗУ — медленнее · {sizeGb:0.0} ГБ", 1);
        }

        return ($"Слишком большая для вашего железа · {sizeGb:0.0} ГБ", 2);
    }
}
