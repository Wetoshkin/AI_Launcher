using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Online;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed partial class ChatViewModel : ViewModelBase
{
    private readonly IChatClient? _injectedClient;
    private CancellationTokenSource? _cts;

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
    private void Stop() => _cts?.Cancel();

    [RelayCommand]
    private void Clear()
    {
        Messages.Clear();
        StatusText = "Очищено.";
    }
}
