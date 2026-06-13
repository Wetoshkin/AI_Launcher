using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Agents.Commands;
using Launcher.Agents.Discovery;
using Launcher.Core.Scenarios;
using Launcher.Desktop.Services;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed record AgentStatusRow(string Name, string Status, bool Installed);

public sealed partial class AgentsViewModel : ViewModelBase
{
    private readonly AgentCliCatalogService _catalog;
    private readonly LocalServerLauncher _serverLauncher = new();
    private readonly Queue<string> _logLines = new();
    private readonly LogStreamServer _logStream = new();
    private SystemHardware? _hardware;

    // ───────────────────────── Модель и параметры запуска ─────────────────────────

    [ObservableProperty]
    private string _runtimeExe = LocalServerLauncher.FindInstalledRuntime() ?? string.Empty;

    [ObservableProperty]
    private string _localModelPath = string.Empty;

    [ObservableProperty]
    private int _serverPort = 8080;

    [ObservableProperty]
    private LaunchPreset _selectedPreset = LaunchPreset.Default;

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

    [ObservableProperty]
    private string _expertArgs = string.Empty;

    [ObservableProperty]
    private string _serverStatus = "Модель не запущена.";

    [ObservableProperty]
    private string _serverLog = string.Empty;

    [ObservableProperty]
    private bool _isServerStarting;

    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private bool _logStreamEnabled;

    [ObservableProperty]
    private int _logStreamPort = 8770;

    [ObservableProperty]
    private string _logStreamStatus = "Лог-стрим выключен.";

    public int MoeMaxLayers => 80;
    public IReadOnlyList<ResponseStyle> Styles => ResponseStyle.All;
    public IReadOnlyList<LaunchPreset> Presets => LaunchPreset.All;

    /// <summary>Делегат выбора GGUF-файла (подставляет App с доступом к окну).</summary>
    public Func<string, IReadOnlyList<string>, Task<string?>>? PickModelAsync { get; set; }

    // ───────────────────────────── Агент и проект ─────────────────────────────

    [ObservableProperty]
    private string _projectFolder = string.Empty;

    [ObservableProperty]
    private AgentKind _selectedAgent = AgentKind.OpenCode;

    [ObservableProperty]
    private string _baseUrl = "http://127.0.0.1:8080/v1";

    [ObservableProperty]
    private string _model = "local-model";

    [ObservableProperty]
    private string _status = "Нажмите «Проверить агенты», чтобы увидеть, что установлено.";

    [ObservableProperty]
    private bool _isModelRunning;

    [ObservableProperty]
    private string _connectionHint =
        "Запустите модель кнопкой выше — её адрес и название подставятся агенту автоматически.";

    public ObservableCollection<AgentStatusRow> Agents { get; } = new();

    public IReadOnlyList<AgentKind> AgentKinds { get; } =
        new[] { AgentKind.OpenCode, AgentKind.Kilo, AgentKind.Claw, AgentKind.Aider, AgentKind.Pi };

    public Func<string, Task<string?>>? PickFolderAsync { get; set; }

    public string Title => "Агенты";
    public string Description =>
        "Запустите модель (движок llama.cpp с выбранными параметрами), затем поднимите кодинг-агента " +
        "(OpenCode, Kilo и др.) в папке проекта — он будет работать через эту модель.";

    public AgentsViewModel()
        : this(new AgentCliCatalogService(new WindowsExecutableResolver()))
    {
    }

    public AgentsViewModel(AgentCliCatalogService catalog)
    {
        _catalog = catalog;
        RunningModel.Instance.Changed += (_, _) => Dispatcher.UIThread.Post(SyncFromRunningModel);
        SyncFromRunningModel();
    }

    // ───────────────────────── Параметры запуска: расчёты ─────────────────────────

    /// <summary>Запоминает железо для авто-MoE и tensor-split.</summary>
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

    private int ComputeAutoMoeLayers()
    {
        if (!IsLikelyMoE(LocalModelPath))
        {
            return 0;
        }

        var vramGb = _hardware is null
            ? 0.0
            : GpuClassifier.UsableVramGb(_hardware, GpuSettings.Instance.UseIntegratedGpu);
        if (vramGb <= 0)
        {
            return 0;
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
            // нет доступа к файлу
        }

        if (modelGb <= 0)
        {
            return 0;
        }

        var layers = modelGb < 8 ? 32 : modelGb < 20 ? 40 : modelGb < 50 ? 60 : 80;
        var expertsGb = modelGb * 0.85;
        var perLayer = expertsGb / layers;
        var overflow = Math.Max(0.0, (modelGb + 2.0) - vramGb * 0.9);
        var n = perLayer <= 0 ? 0 : (int)Math.Ceiling(overflow / perLayer);
        return Math.Clamp(n, 0, layers);
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
            parts.Add($"--n-cpu-moe {MoeCpuLayers}");
        }

        if (Reasoning)
        {
            parts.Add($"--reasoning on --reasoning-format deepseek-legacy --reasoning-budget {ReasoningBudget()}");
        }

        if (!string.IsNullOrWhiteSpace(ExpertArgs))
        {
            parts.Add(ExpertArgs.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    private string? ComputeTensorSplit()
    {
        if (_hardware is null)
        {
            return null;
        }

        var cards = GpuClassifier.UsableGpus(_hardware, GpuSettings.Instance.UseIntegratedGpu);
        return cards.Count < 2
            ? null
            : string.Join(",", cards.Select(g => ((int)Math.Round(Math.Max(1.0, g.TotalGb))).ToString()));
    }

    // ───────────────────────── Запуск/остановка модели ─────────────────────────

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
        ServerStatus = "Запускаем движок и загружаем модель в память…";
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
            _logStream.SetModel(result.Model);

            if (result.Ready)
            {
                RunningModel.Instance.Set(result.BaseUrl, result.Model);
                ServerStatus = "✓ Модель запущена и готова. Теперь запустите агента ниже.";
            }
            else
            {
                ServerStatus = "⚠ " + result.Message;
            }
        }
        catch (Exception ex)
        {
            ServerStatus = "Ошибка запуска модели: " + ex.Message;
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
        RunningModel.Instance.Clear();
        ServerStatus = "Модель остановлена.";
    }

    private void AppendServerLog(string line)
    {
        _logStream.Append(line);
        Dispatcher.UIThread.Post(() =>
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

    partial void OnLogStreamEnabledChanged(bool value)
    {
        if (value)
        {
            if (_logStream.Start(LogStreamPort))
            {
                LogStreamStatus = $"Лог-стрим работает: {_logStream.Url} (откройте в браузере с любого устройства этого ПК)";
            }
            else
            {
                LogStreamStatus = "Не удалось запустить лог-стрим (порт занят?). Смените порт.";
                _logStreamEnabled = false;
                OnPropertyChanged(nameof(LogStreamEnabled));
            }
        }
        else
        {
            _logStream.Stop();
            LogStreamStatus = "Лог-стрим выключен.";
        }
    }

    [RelayCommand]
    private void OpenLogStream()
    {
        if (!_logStream.IsRunning)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_logStream.Url) { UseShellExecute = true });
        }
        catch
        {
            // браузер не открылся
        }
    }

    // ───────────────────────── Подключение агента к модели ─────────────────────────

    private void SyncFromRunningModel()
    {
        var running = RunningModel.Instance;
        IsModelRunning = running.IsRunning;

        if (running.IsRunning)
        {
            BaseUrl = NormalizeAgentBaseUrl(running.BaseUrl);
            Model = string.IsNullOrWhiteSpace(running.ModelId) ? "local-model" : running.ModelId;
            ConnectionHint = $"✓ Подключено к запущенной модели: {Model}  ({BaseUrl}). Агент будет отвечать через неё.";
        }
        else
        {
            ConnectionHint = "Модель не запущена. Запустите её кнопкой выше — адрес и название подставятся агенту " +
                "автоматически. Либо впишите онлайн-адрес и ключ вручную.";
        }
    }

    private static string NormalizeAgentBaseUrl(string baseUrl)
    {
        var url = baseUrl.TrimEnd('/');
        return url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? url : url + "/v1";
    }

    [RelayCommand]
    private async Task CheckAgentsAsync()
    {
        Status = "Проверяем установленные агенты…";
        Agents.Clear();

        try
        {
            var statuses = await _catalog.CheckAsync(CancellationToken.None);
            foreach (var s in statuses)
            {
                Agents.Add(new AgentStatusRow(
                    $"{s.Agent} ({s.ExecutableName})",
                    s.IsInstalled ? $"✓ {s.StatusText}" : "✗ не установлен",
                    s.IsInstalled));
            }

            Status = "Готово. Не установленный агент можно поставить через npm/pipx — см. его документацию.";
        }
        catch (Exception ex)
        {
            Status = "Не удалось проверить агенты: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task BrowseProjectAsync()
    {
        if (PickFolderAsync is null)
        {
            return;
        }

        var folder = await PickFolderAsync("Папка проекта");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            ProjectFolder = folder;
        }
    }

    [RelayCommand]
    private async Task PrepareAndLaunchAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectFolder))
        {
            Status = "Сначала выберите папку проекта.";
            return;
        }

        var request = new AgentLaunchRequest(SelectedAgent, ProjectFolder.Trim(), Model.Trim(), BaseUrl.Trim());

        try
        {
            var config = await new AgentProjectConfigWriter().WriteAsync(request, CancellationToken.None);

            var isLocalUrl = BaseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);
            if (isLocalUrl && !IsModelRunning)
            {
                Status = $"Конфиг записан ({config.Message}), но локальная модель не запущена — агенту не к чему подключиться. " +
                    "Запустите модель кнопкой выше и повторите.";
                return;
            }

            var plan = BuildPlan(request);

            var psi = new ProcessStartInfo(plan.Executable)
            {
                UseShellExecute = true,
                WorkingDirectory = ProjectFolder.Trim(),
            };
            foreach (var arg in plan.Arguments)
            {
                psi.ArgumentList.Add(arg);
            }

            try
            {
                Process.Start(psi);
                Status = $"Конфиг: {config.Message} Запущен агент «{SelectedAgent}» в новом окне.";
            }
            catch (Exception)
            {
                Status = $"Конфиг записан ({config.Message}), но агент «{SelectedAgent}» не найден в PATH. " +
                         "Установите его CLI и повторите. Команда: " + plan.Executable + " " + string.Join(' ', plan.Arguments);
            }
        }
        catch (Exception ex)
        {
            Status = "Ошибка подготовки: " + ex.Message;
        }
    }

    private static Launcher.Core.LaunchPlans.LaunchPlan BuildPlan(AgentLaunchRequest request)
    {
        IAgentCommandBuilder builder = request.Agent switch
        {
            AgentKind.OpenCode => new OpenCodeCommandBuilder(),
            AgentKind.Kilo => new KiloCommandBuilder(),
            AgentKind.Claw => new ClawCommandBuilder(),
            AgentKind.Aider => new AiderCommandBuilder(),
            _ => new OpenCodeCommandBuilder(),
        };
        return builder.Build(request);
    }
}
