using System;

namespace Launcher.Desktop.Services;

/// <summary>
/// Общий стейт запущенного локального сервера модели: адрес и id модели.
/// Чат публикует его при старте сервера, Агенты и другие экраны читают, чтобы подключиться к той же модели.
/// </summary>
public sealed class RunningModel
{
    public static RunningModel Instance { get; } = new();

    public bool IsRunning { get; private set; }
    public string BaseUrl { get; private set; } = string.Empty;
    public string ModelId { get; private set; } = string.Empty;

    public event EventHandler? Changed;

    public void Set(string baseUrl, string modelId)
    {
        IsRunning = true;
        BaseUrl = baseUrl;
        ModelId = modelId;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        IsRunning = false;
        BaseUrl = string.Empty;
        ModelId = string.Empty;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
