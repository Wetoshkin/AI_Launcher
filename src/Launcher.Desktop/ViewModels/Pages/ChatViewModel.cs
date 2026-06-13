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
    public void ApplyHardware(SystemHardware hardware)
    {
        _hardware = hardware;
        RecomputeMoe();
    }

    partial void OnLocalModelPathChanged(string value) => RecomputeMoe();

    partial void OnMoeAutoChanged(bool value) => RecomputeMoe();

    private void RecomputeMoe()
    {
        if (MoeAuto)
        {
            MoeCpuLayers = ComputeAutoMoeLayers();
        }
    }

    /// <summary>
    /// Авто-расчёт числа MoE-слоёв на CPU: ноль если модель влезает в VRAM,
    /// иначе столько слоёв экспертов, чтобы поместиться. Эвристика по размеру файла и VRAM.
    /// </summary>
    private int ComputeAutoMoeLayers()
    {
        if (!IsLikelyMoE(LocalModelPath))
        {
            return 0;
        }

        var vramGb = _hardware?.Gpus.Sum(g => g.TotalGb) ?? 0.0;
        if (vramGb <= 0)
        {
            return 0; // только CPU — разгружать нечего
        }

        double modelGb = 0;
        try
        {
            if (System.IO.File.Exists(LocalModelPath))
            {
                modelGb = new System.IO.FileInfo(LocalModelPath).Length / 1024.0 / 1024.0 / 1024.0;
            }
        }
        catch
        {
            // нет доступа к файлу — оставим 0
        }

        if (modelGb <= 0)
        {
            return 0;
        }

        var layers = modelGb < 8 ? 32 : modelGb < 20 ? 40 : modelGb < 50 ? 60 : 80;
        var expertsGb = modelGb * 0.85;
        var perLayer = expertsGb / layers;
        var overflow = System.Math.Max(0.0, (modelGb + 2.0) - vramGb * 0.9);
        var n = perLayer <= 0 ? 0 : (int)System.Math.Ceiling(overflow / perLayer);
        return System.Math.Clamp(n, 0, layers);
    }

    private int ReasoningBudget() => ReasoningDepthIndex switch
    {
        1 => 2048,
        2 => 512,
        _ => -1,
    };

    /// <summary>Эвристика: похоже ли имя GGUF на модель-смесь экспертов (MoE).</summary>
    public static bool IsLikelyMoE(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var name = System.IO.Path.GetFileName(path).ToLowerInvariant();
        string[] markers =
        {
            "moe", "mixtral", "8x7b", "8x22b", "qwen3-next", "-a3b", "-a13b", "-a22b",
            "a3b", "a13b", "a22b", "30b-a", "235b-a", "glm-4.5", "glm-4.6", "glm-5", "scout", "maverick",
        };
        return markers.Any(m => name.Contains(m));
    }

    [ObservableProperty]
    private string _runtimeExe = LocalServerLauncher.FindInstalledRuntime() ?? string.Empty;

    [ObservableProperty]
    private string _localModelPath = string.Empty;

    [ObservableProperty]
    private int _serverPort = 8080;

    [ObservableProperty]
    private LaunchPreset _selectedPreset = LaunchPreset.Default;

    [ObservableProperty]
    private string _expertArgs = string.Empty;

    [ObservableProperty]
    private string _systemPrompt = string.Empty;

    [ObservableProperty]
    private ResponseStyle _selectedStyle = ResponseStyle.All[0];

    [ObservableProperty]
    private bool _saveMemory;

    [ObservableProperty]
    private bool _moeAuto = true;

    [ObservableProperty]
    private int _moeCpuLayers;

    [ObservableProperty]
    private bool _reasoning;

    [ObservableProperty]
    private int _reasoningDepthIndex;

    public int MoeMaxLayers => 80;

    public IReadOnlyList<ResponseStyle> Styles => ResponseStyle.All;

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

    private IReadOnlyList<ChatMessage> BuildRequestMessages()
    {
        var list = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
        {
            list.Add(new ChatMessage("system", SystemPrompt.Trim()));
        }

        list.AddRange(Messages.Select(m => new ChatMessage(m.Role, m.Content)));
        return list;
    }

    /// <summary>Собирает дополнительные аргументы сервера: стиль + экономия памяти + ручные (Эксперт).</summary>
    private string? ComposeServerArgs()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(SelectedStyle.Args))
        {
            parts.Add(SelectedStyle.Args);
        }

        if (SaveMemory)
        {
            parts.Add("--cache-type-k q8_0 --cache-type-v q8_0 --flash-attn on");
        }

        if (MoeCpuLayers > 0)
        {
            // MoE: держать экспертов первых N слоёв на CPU — большие MoE влезают в VRAM.
            parts.Add($"--n-cpu-moe {MoeCpuLayers}");
        }

        if (Reasoning)
        {
            // Reasoning-модели: включить размышления с бюджетом; think-теги остаются в тексте.
            parts.Add($"--reasoning on --reasoning-format deepseek-legacy --reasoning-budget {ReasoningBudget()}");
        }

        if (!string.IsNullOrWhiteSpace(ExpertArgs))
        {
            parts.Add(ExpertArgs.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

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
                CancellationToken.None,
                antiLoop: true,
                tensorSplit: ComputeTensorSplit(),
                extraArgs: ComposeServerArgs());

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

    /// <summary>Tensor-split пропорционально VRAM при двух+ видеокартах (например 3090+3060 → 24,12).</summary>
    private string? ComputeTensorSplit()
    {
        if (_hardware is null || _hardware.Gpus.Count < 2)
        {
            return null;
        }

        return string.Join(",", _hardware.Gpus.Select(g => ((int)System.Math.Round(System.Math.Max(1.0, g.TotalGb))).ToString()));
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
