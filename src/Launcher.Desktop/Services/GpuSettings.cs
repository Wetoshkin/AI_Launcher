using System;

namespace Launcher.Desktop.Services;

/// <summary>
/// Общая настройка: учитывать ли встроенную видеокарту (iGPU) в расчётах и распределении модели.
/// По умолчанию выключена — встройка работает только на отрисовку Windows.
/// Экраны (сайдбар, модели, чат) подписываются на Changed, чтобы пересчитать бюджет видеопамяти.
/// </summary>
public sealed class GpuSettings
{
    public static GpuSettings Instance { get; } = new();

    private bool _useIntegratedGpu;

    public bool UseIntegratedGpu
    {
        get => _useIntegratedGpu;
        set
        {
            if (_useIntegratedGpu == value)
            {
                return;
            }

            _useIntegratedGpu = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? Changed;
}
