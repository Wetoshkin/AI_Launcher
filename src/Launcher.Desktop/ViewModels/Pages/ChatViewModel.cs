using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Desktop.Services;
using Launcher.Online;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class ChatViewModel : ViewModelBase
{
    private readonly IChatClient? _injectedClient;
    private readonly LocalServerLauncher _serverLauncher = new();
    private CancellationTokenSource? _cts;

    private SystemHardware? _hardware;

    /// <summary>Запоминает железо для подсказок по мульти-GPU при запуске.</summary>
    public void ApplyHardware(SystemHardware hardware) => _hardware = hardware;

    [ObservableProperty]
    private string _runtimeExe = LocalServerLauncher.FindInstalledRuntime() ?? string.Empty;

    [ObservableProperty]
    private string _localModelPath = string.Empty;

    [ObservableProperty]
    private int _serverPort = 8080;

    [ObservableProperty]
    private LaunchPreset _selectedPreset = LaunchPreset.Default;

    public IReadOnlyList<LaunchPreset> Presets => LaunchPreset.All;

    [ObservableProperty]
    private string _serverStatus = "Локальный сервер не запущен.";

    [ObservableProperty]
    private string _serverLog = string.Empty;

    private readonly System.Collections.Generic.Queue<string> _logLines = new();

    [ObservableProperty]
    private bool _isServerStarting;

    [ObservableProperty]
    private bool _isServerRunning;

    /// <summary>Делегат выбора файла модели (подставляет App с доступом к окну).</summary>
    public Func<string, IReadOnlyList<string>, Task<string?>>? PickModelAsync { get; set; }

    [ObservableProperty]
    private string _input = string.Empty;

    [ObservableProperty]
    private ProviderPreset _selectedProvider = ProviderRegistry.Default;

    [ObservableProperty]
    private string _baseUrl = "http://127.0.0.1:8080/v1";

    [ObservableProperty]
    private string _model = "local-model";

    [ObservableProperty]
    private string? _apiKey;

    [ObservableProperty]
    private bool _useProxy;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Готов. Запустите локальную модель или выберите онлайн-провайдера.";

    public ObservableCollection<ChatMessageViewModel> Messages { get; } = new();

    public IReadOnlyList<ProviderPreset> Providers => ProviderRegistry.All;

    public string Title => "Чат";
    public string Description => "Общение с локальной или онлайн моделью.";

    public bool KeyRequired => SelectedProvider.RequiresKey;

    public ChatViewModel()
    {
        _injectedClient = null;
    }

    public ChatViewModel(IChatClient client)
    {
        _injectedClient = client;
    }

    /// <summary>Настроить чат на конкретный endpoint (например после запуска локального сервера).</summary>
    public void UseEndpoint(string baseUrl, string model, string? apiKey = null)
    {
        BaseUrl = baseUrl;
        Model = model;
        ApiKey = apiKey;
        StatusText = $"Подключено к {model}.";
    }

    partial void OnSelectedProviderChanged(ProviderPreset value)
    {
        BaseUrl = value.BaseUrl;
        Model = value.DefaultModel;
        OnPropertyChanged(nameof(KeyRequired));
        StatusText = value.RequiresKey
            ? $"{value.DisplayName}: введите API-ключ."
            : $"{value.DisplayName}: ключ не нужен.";
    }

    private bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(Input);

    partial void OnIsBusyChanged(bool value) => SendCommand.NotifyCanExecuteChanged();

    partial void OnInputChanged(string value) => SendCommand.NotifyCanExecuteChanged();

    private IChatClient ResolveClient()
    {
        if (_injectedClient is not null)
        {
            return _injectedClient;
        }

        var proxy = ProxySettings.Hiddify with { Enabled = UseProxy };
        return ChatClientFactory.Create(SelectedProvider, proxy);
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        var text = Input.Trim();
        if (text.Length == 0)
        {
            return;
        }

        Input = string.Empty;
        Messages.Add(new ChatMessageViewModel("user", text, isUser: true));

        var request = BuildRequestMessages();
        var assistant = new ChatMessageViewModel("assistant", string.Empty, isUser: false);
        Messages.Add(assistant);

        IsBusy = true;
        StatusText = "Генерация ответа…";
        _cts = new CancellationTokenSource();

        try
        {
            var endpoint = new ChatEndpoint(BaseUrl.Trim(), Model.Trim(),
                string.IsNullOrWhiteSpace(ApiKey) ? null : ApiKey!.Trim());

            await foreach (var token in ResolveClient().StreamAsync(endpoint, request, _cts.Token))
            {
                assistant.Append(token);
            }

            if (assistant.Content.Length == 0)
            {
                assistant.Content = "(пустой ответ)";
            }

            StatusText = "Готово.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Остановлено.";
            if (assistant.Content.Length == 0)
            {
                assistant.Content = "(остановлено)";
            }
        }
        catch (Exception ex)
        {
            assistant.Content = "⚠ Не удалось получить ответ: " + ex.Message;
            StatusText = "Ошибка соединения. Проверьте адрес сервера, ключ и прокси.";
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private IReadOnlyList<ChatMessage> BuildRequestMessages() =>
        Messages.Select(m => new ChatMessage(m.Role, m.Content)).ToList();

    [RelayCommand]
    private async Task BrowseModelAsync()
    {
        if (PickModelAsync is null)
        {
            return;
        }

        var path = await PickModelAsync("Выберите GGUF-модель", new[] { ".gguf" });
        if (!string.IsNullOrWhiteSpace(path))
        {
            LocalModelPath = path;
        }
    }

    [RelayCommand]
    private async Task StartServerAsync()
    {
        if (IsServerStarting)
        {
            return;
        }

        IsServerStarting = true;
        ServerStatus = "Запускаем локальный сервер и загружаем модель в память…";
        _logLines.Clear();
        ServerLog = string.Empty;

        try
        {
            var result = await _serverLauncher.StartAsync(
                RuntimeExe.Trim(),
                LocalModelPath.Trim(),
                ServerPort,
                contextTokens: SelectedPreset.ContextTokens,
                log: AppendServerLog,
                CancellationToken.None);

            IsServerRunning = _serverLauncher.IsRunning;

            if (result.Ready)
            {
                SelectedProvider = ProviderRegistry.ForKind(ProviderKind.Local);
                UseEndpoint(result.BaseUrl, result.Model);
                ServerStatus = "✓ Локальный сервер готов. Можно писать сообщения.";
            }
            else
            {
                ServerStatus = "⚠ " + result.Message;
            }
        }
        catch (Exception ex)
        {
            ServerStatus = "Ошибка запуска сервера: " + ex.Message;
        }
        finally
        {
            IsServerStarting = false;
        }
    }

    [RelayCommand]
    private void StopServer()
    {
        _serverLauncher.Stop();
        IsServerRunning = false;
        ServerStatus = "Локальный сервер остановлен.";
    }

    /// <summary>Дописывает строку лога сервера (с маршалингом в UI-поток, с ограничением размера).</summary>
    private void AppendServerLog(string line)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _logLines.Enqueue(line);
            while (_logLines.Count > 500)
            {
                _logLines.Dequeue();
            }

            ServerLog = string.Join("\n", _logLines);
        });
    }

    [RelayCommand]
    private void ClearLog()
    {
        _logLines.Clear();
        ServerLog = string.Empty;
    }

    [RelayCommand]
    private void Stop() => _cts?.Cancel();

    [RelayCommand]
    private void Clear()
    {
        Messages.Clear();
        StatusText = "Очищено.";
    }
}
