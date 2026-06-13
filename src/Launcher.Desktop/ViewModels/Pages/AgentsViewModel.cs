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
using Launcher.Models.Catalog;
using Launcher.Runtimes.Hardware;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed record AgentStatusRow(string Name, string Status, bool Installed);

/// <summary>Строка прогноза: сколько памяти займёт модель на конкретной карте (или в ОЗУ).</summary>
public sealed record GpuFillRow(string Name, string Text, double Percent, int Level);

/// <summary>Локальная модель в выпадающем списке (часто используемые — выше).</summary>
public sealed record LocalModelOption(string Display, string Path)
{
    public override string ToString() => Display;
}

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
    private int _contextTokens = 32768;

    [ObservableProperty]
    private int _modelNativeContext;

    [ObservableProperty]
    private string _contextHint = "У каждой модели свой «родной» контекст — он подставится при выборе модели.";

    [ObservableProperty]
    private ResponseStyle _selectedStyle = ResponseStyle.All[0];

    [ObservableProperty]
    private double _temperature;

    [ObservableProperty]
    private KvCacheMode _selectedKvCache = KvCacheMode.Default;

    [ObservableProperty]
    private string _commandPreview = string.Empty;

    [ObservableProperty]
    private bool _moeAuto = true;

    [ObservableProperty]
    private int _moeCpuLayers;

    [ObservableProperty]
    private bool _reasoning;

    [ObservableProperty]
    private int _reasoningDepthIndex;

    [ObservableProperty]
    private bool _flashAttention;

    [ObservableProperty]
    private bool _keepInRam;

    [ObservableProperty]
    private bool _noMmap;

    [ObservableProperty]
    private bool _splitModeRow;

    [ObservableProperty]
    private bool _verboseLog;

    [ObservableProperty]
    private bool _showExpert;

    [ObservableProperty]
    private bool _showEngine;

    [ObservableProperty]
    private string _draftModelPath = string.Empty;

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
    public IReadOnlyList<KvCacheMode> KvCacheModes => KvCacheMode.All;

    public ObservableCollection<LocalModelOption> LocalModels { get; } = new();

    [ObservableProperty]
    private LocalModelOption? _selectedLocalModel;

    public ObservableCollection<GpuFillRow> GpuFill { get; } = new();

    [ObservableProperty]
    private string _gpuFillSummary = "Выберите модель — покажу, как она ляжет в память.";

    [ObservableProperty]
    private bool _hasGpuFill;

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
    private string _model = "local/model";

    [ObservableProperty]
    private string _status = "Нажмите «Проверить агенты», чтобы увидеть, что установлено.";

    [ObservableProperty]
    private bool _isModelRunning;

    [ObservableProperty]
    private string _connectionHint =
        "Модель не запущена. Нажмите «🚀 Старт» ниже — адрес и название подставятся автоматически.";

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
        GpuSettings.Instance.Changed += (_, _) => Dispatcher.UIThread.Post(RecomputeGpuFill);
        SyncFromRunningModel();
        RefreshLocalModels();
        RecomputeCommandPreview();
    }

    /// <summary>Список локальных моделей из запомненной папки; часто используемые — выше.</summary>
    [RelayCommand]
    public void RefreshLocalModels()
    {
        var prefs = UiPreferences.Load();
        var folder = prefs.ModelsFolder;

        var keepPath = LocalModelPath;
        LocalModels.Clear();

        if (string.IsNullOrWhiteSpace(folder) || !System.IO.Directory.Exists(folder))
        {
            return;
        }

        var recent = prefs.RecentModels ?? new List<string>();

        try
        {
            var found = LocalModelCatalog.Scan(new[] { folder });
            var ordered = found
                .OrderBy(m =>
                {
                    var idx = recent.FindIndex(p => string.Equals(p, System.IO.Path.GetFullPath(m.Path), StringComparison.OrdinalIgnoreCase));
                    return idx < 0 ? int.MaxValue : idx;
                })
                .ThenBy(m => System.IO.Path.GetFileName(m.Path), StringComparer.OrdinalIgnoreCase);

            foreach (var m in ordered)
            {
                LocalModels.Add(new LocalModelOption(
                    $"{System.IO.Path.GetFileName(m.Path)}  ·  {m.SizeGb:0.0} ГБ",
                    System.IO.Path.GetFullPath(m.Path)));
            }
        }
        catch
        {
            // папка недоступна — список пуст
        }

        if (!string.IsNullOrWhiteSpace(keepPath))
        {
            SelectedLocalModel = LocalModels.FirstOrDefault(o =>
                string.Equals(o.Path, System.IO.Path.GetFullPath(keepPath), StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void BumpRecentModel(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var full = System.IO.Path.GetFullPath(path);
        var prefs = UiPreferences.Load();
        prefs.RecentModels ??= new List<string>();
        prefs.RecentModels.RemoveAll(p => string.Equals(p, full, StringComparison.OrdinalIgnoreCase));
        prefs.RecentModels.Insert(0, full);
        if (prefs.RecentModels.Count > 20)
        {
            prefs.RecentModels.RemoveRange(20, prefs.RecentModels.Count - 20);
        }

        prefs.Save();
    }

    private LaunchProfile CaptureProfile() => new()
    {
        ModelPath = LocalModelPath,
        ContextTokens = ContextTokens,
        KvModeIndex = System.Math.Max(0, KvCacheMode.All.ToList().IndexOf(SelectedKvCache)),
        Agent = SelectedAgent.ToString(),
        ProjectFolder = ProjectFolder,
        Style = SelectedStyle.Name,
        Temperature = Temperature,
        MoeAuto = MoeAuto,
        MoeCpuLayers = MoeCpuLayers,
        Reasoning = Reasoning,
        ReasoningDepth = ReasoningDepthIndex,
        FlashAttention = FlashAttention,
        KeepInRam = KeepInRam,
        NoMmap = NoMmap,
        SplitModeRow = SplitModeRow,
        VerboseLog = VerboseLog,
        ExpertArgs = ExpertArgs,
        Port = ServerPort,
    };

    private void SaveLastLaunch()
    {
        var prefs = UiPreferences.Load();
        prefs.LastLaunch = CaptureProfile();
        prefs.Save();
    }

    /// <summary>Применить сохранённый профиль (быстрый повтор последнего запуска с Главной).</summary>
    public void ApplyProfile(LaunchProfile p)
    {
        LocalModelPath = p.ModelPath;
        SelectedPreset = Presets.FirstOrDefault(x => x.ContextTokens == p.ContextTokens) ?? LaunchPreset.Default;
        SelectedKvCache = p.KvModeIndex >= 0 && p.KvModeIndex < KvCacheMode.All.Count
            ? KvCacheMode.All[p.KvModeIndex]
            : KvCacheMode.Default;
        if (System.Enum.TryParse<AgentKind>(p.Agent, out var ak))
        {
            SelectedAgent = ak;
        }

        ContextTokens = p.ContextTokens;
        ProjectFolder = p.ProjectFolder;
        SelectedStyle = Styles.FirstOrDefault(s => s.Name == p.Style) ?? Styles[0];
        Temperature = p.Temperature;
        MoeAuto = p.MoeAuto;
        if (!p.MoeAuto)
        {
            MoeCpuLayers = p.MoeCpuLayers;
        }

        Reasoning = p.Reasoning;
        ReasoningDepthIndex = p.ReasoningDepth;
        FlashAttention = p.FlashAttention;
        KeepInRam = p.KeepInRam;
        NoMmap = p.NoMmap;
        SplitModeRow = p.SplitModeRow;
        VerboseLog = p.VerboseLog;
        ExpertArgs = p.ExpertArgs;
        ServerPort = p.Port;
        RefreshLocalModels();
    }

    // ───────────────────────── Параметры запуска: расчёты ─────────────────────────

    /// <summary>Запоминает железо для авто-MoE и tensor-split.</summary>
    public void ApplyHardware(SystemHardware hardware)
    {
        _hardware = hardware;
        RecomputeMoe();
        RecomputeGpuFill();
    }

    partial void OnLocalModelPathChanged(string value)
    {
        RecomputeMoe();
        LoadNativeContext(value);
        UpdateDerived();
    }

    partial void OnMoeAutoChanged(bool value) => RecomputeMoe();

    partial void OnMoeCpuLayersChanged(int value) => UpdateDerived();

    partial void OnContextTokensChanged(int value) => UpdateDerived();

    partial void OnSelectedPresetChanged(LaunchPreset value) => ContextTokens = value.ContextTokens;

    partial void OnSelectedKvCacheChanged(KvCacheMode value) => UpdateDerived();

    partial void OnSelectedStyleChanged(ResponseStyle value) => RecomputeCommandPreview();

    partial void OnTemperatureChanged(double value) => RecomputeCommandPreview();

    partial void OnReasoningChanged(bool value) => RecomputeCommandPreview();

    partial void OnReasoningDepthIndexChanged(int value) => RecomputeCommandPreview();

    partial void OnFlashAttentionChanged(bool value) => RecomputeCommandPreview();

    partial void OnKeepInRamChanged(bool value) => RecomputeCommandPreview();

    partial void OnNoMmapChanged(bool value) => RecomputeCommandPreview();

    partial void OnSplitModeRowChanged(bool value) => RecomputeCommandPreview();

    partial void OnVerboseLogChanged(bool value) => RecomputeCommandPreview();

    partial void OnExpertArgsChanged(string value) => RecomputeCommandPreview();

    partial void OnDraftModelPathChanged(string value) => RecomputeCommandPreview();

    partial void OnServerPortChanged(int value) => RecomputeCommandPreview();

    partial void OnSelectedLocalModelChanged(LocalModelOption? value)
    {
        if (value is not null && !string.IsNullOrWhiteSpace(value.Path))
        {
            LocalModelPath = value.Path;
        }
    }

    private void UpdateDerived()
    {
        RecomputeGpuFill();
        RecomputeCommandPreview();
    }

    /// <summary>Читает «родной» контекст модели из GGUF и подставляет его по умолчанию.</summary>
    private void LoadNativeContext(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            ModelNativeContext = 0;
            ContextHint = "У каждой модели свой «родной» контекст — он подставится при выборе модели.";
            return;
        }

        Task.Run(() => GgufMetadata.ReadContextLength(path)).ContinueWith(t =>
        {
            var native = t.Status == TaskStatus.RanToCompletion ? t.Result : null;
            Dispatcher.UIThread.Post(() =>
            {
                if (native is > 0)
                {
                    ModelNativeContext = native.Value;
                    ContextTokens = System.Math.Min(native.Value, 32768);
                    ContextHint = $"Родной контекст модели — {native.Value / 1024}K. " +
                        "Меньше ставить можно (экономит память), больше — только если хватает видеопамяти (модель может работать хуже).";
                }
                else
                {
                    ModelNativeContext = 0;
                    ContextHint = "Не удалось определить родной контекст модели. Ставьте по памяти: для агентов 32K — хороший старт.";
                }
            });
        });
    }

    private void RecomputeCommandPreview()
    {
        var exe = string.IsNullOrWhiteSpace(RuntimeExe) ? "llama-server" : System.IO.Path.GetFileName(RuntimeExe);
        var model = string.IsNullOrWhiteSpace(LocalModelPath) ? "<модель>" : System.IO.Path.GetFileName(LocalModelPath);
        var alias = "local/" + System.IO.Path.GetFileNameWithoutExtension(
            string.IsNullOrWhiteSpace(LocalModelPath) ? "model" : LocalModelPath);

        var sb = new System.Text.StringBuilder();
        sb.Append($"{exe} -m \"{model}\" --alias {alias} --ctx-size {ContextTokens} --port {ServerPort} --host 127.0.0.1 --dry-multiplier 0.8");
        var extra = ComposeServerArgs();
        if (!string.IsNullOrWhiteSpace(extra))
        {
            sb.Append(' ').Append(extra);
        }

        CommandPreview = sb.ToString();
    }

    private void RecomputeMoe()
    {
        if (MoeAuto)
        {
            MoeCpuLayers = ComputeAutoMoeLayers();
        }
    }

    /// <summary>Прогноз: как модель (вес + KV-кэш) распределится по видеопамяти и ОЗУ.</summary>
    private void RecomputeGpuFill()
    {
        GpuFill.Clear();

        var modelGb = FileGb(LocalModelPath);
        if (modelGb <= 0 || _hardware is null)
        {
            HasGpuFill = false;
            GpuFillSummary = "Выберите модель — покажу, как она ляжет в память.";
            return;
        }

        // KV-кэш ≈ 0.125 МБ на токен (f16, ориентир для 7–8B), множитель режима сжатия.
        var kvGb = ContextTokens * 0.125 / 1024.0 * SelectedKvCache.Factor;
        var need = modelGb + kvGb;

        // MoE: эксперты первых N слоёв уходят на CPU → в ОЗУ.
        double cpuOffloadGb = 0;
        if (MoeCpuLayers > 0 && IsLikelyMoE(LocalModelPath))
        {
            var layers = modelGb < 8 ? 32 : modelGb < 20 ? 40 : modelGb < 50 ? 60 : 80;
            cpuOffloadGb = Math.Min(modelGb * 0.85, modelGb * 0.85 / layers * MoeCpuLayers);
        }

        var gpuNeed = Math.Max(0, need - cpuOffloadGb);
        var ramUsed = cpuOffloadGb;

        var gpus = GpuClassifier.UsableGpus(_hardware, GpuSettings.Instance.UseIntegratedGpu)
            .OrderByDescending(g => g.TotalGb)
            .ToList();
        var sumV = gpus.Sum(g => g.TotalGb);

        if (sumV <= 0)
        {
            ramUsed += gpuNeed;
        }
        else if (gpuNeed <= sumV)
        {
            foreach (var g in gpus)
            {
                AddGpuRow(g.Name, gpuNeed * g.TotalGb / sumV, g.TotalGb);
            }
        }
        else
        {
            foreach (var g in gpus)
            {
                AddGpuRow(g.Name, g.TotalGb, g.TotalGb);
            }

            ramUsed += gpuNeed - sumV;
        }

        if (ramUsed > 0.05)
        {
            var ramTotal = _hardware.RamTotalGb;
            var pct = ramTotal > 0 ? Math.Min(100.0, ramUsed / ramTotal * 100.0) : 0;
            GpuFill.Add(new GpuFillRow("ОЗУ (CPU)", $"{ramUsed:0.0} из {ramTotal:0.0} ГБ", pct, 1));
        }

        HasGpuFill = GpuFill.Count > 0;

        var ctxText = $"контекст {ContextTokens / 1024}K, KV ~{kvGb:0.0} ГБ ({SelectedKvCache.Name.Split(' ')[0]})";
        if (ramUsed <= 0.05 && sumV > 0)
        {
            GpuFillSummary = $"Модель целиком в видеопамяти (~{need:0.0} ГБ; {ctxText}) — будет быстро.";
        }
        else if (sumV > 0)
        {
            GpuFillSummary = $"~{ramUsed:0.0} ГБ уйдёт в ОЗУ (offload; {ctxText}) — медленнее, но запустится.";
        }
        else
        {
            GpuFillSummary = $"Только CPU/ОЗУ (~{need:0.0} ГБ; {ctxText}) — без видеокарты будет медленно.";
        }
    }

    private void AddGpuRow(string name, double usedGb, double totalGb)
    {
        var pct = totalGb > 0 ? Math.Min(100.0, usedGb / totalGb * 100.0) : 0;
        var level = pct <= 90 ? 0 : pct < 100 ? 1 : 2;
        GpuFill.Add(new GpuFillRow(ShortName(name), $"{usedGb:0.0} из {totalGb:0.0} ГБ", pct, level));
    }

    private static double FileGb(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
            {
                return new System.IO.FileInfo(path).Length / 1024.0 / 1024.0 / 1024.0;
            }
        }
        catch
        {
            // нет доступа
        }

        return 0;
    }

    private static string ShortName(string name) => name
        .Replace("NVIDIA GeForce ", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Replace("NVIDIA ", string.Empty, StringComparison.OrdinalIgnoreCase)
        .Replace("(R)", string.Empty).Replace("(TM)", string.Empty)
        .Trim();

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

        // Ручная температура переопределяет значение из стиля (llama-server берёт последнее).
        if (Temperature > 0)
        {
            parts.Add("--temp " + Temperature.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(SelectedKvCache.Args))
        {
            parts.Add(SelectedKvCache.Args!);
        }

        // Flash Attention — один раз: нужен для сжатого KV или включён вручную.
        if (SelectedKvCache.RequiresFlashAttention || FlashAttention)
        {
            parts.Add("--flash-attn on");
        }

        if (MoeCpuLayers > 0)
        {
            parts.Add($"--n-cpu-moe {MoeCpuLayers}");
        }

        if (Reasoning)
        {
            parts.Add($"--reasoning on --reasoning-format deepseek-legacy --reasoning-budget {ReasoningBudget()}");
        }

        if (KeepInRam)
        {
            parts.Add("--mlock");
        }

        if (NoMmap)
        {
            parts.Add("--no-mmap");
        }

        if (SplitModeRow)
        {
            parts.Add("--split-mode row");
        }

        if (VerboseLog)
        {
            parts.Add("--verbose");
        }

        if (!string.IsNullOrWhiteSpace(DraftModelPath))
        {
            parts.Add($"--model-draft \"{DraftModelPath.Trim()}\"");
        }

        if (!string.IsNullOrWhiteSpace(ExpertArgs))
        {
            parts.Add(ExpertArgs.Trim());
        }

        return parts.Count == 0 ? null : string.Join(" ", parts);
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
                contextTokens: ContextTokens,
                log: AppendServerLog,
                CancellationToken.None,
                antiLoop: true,
                // Свой --tensor-split НЕ передаём: он отключает авто-подгонку llama.cpp под
                // свободную память и приводит к OOM. llama.cpp сам распределит по видеокартам.
                tensorSplit: null,
                extraArgs: ComposeServerArgs());

            IsServerRunning = _serverLauncher.IsRunning;
            _logStream.SetModel(result.Model);

            if (result.Ready)
            {
                RunningModel.Instance.Set(result.BaseUrl, result.Model);
                BumpRecentModel(LocalModelPath);
                RefreshLocalModels();
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
            Model = string.IsNullOrWhiteSpace(running.ModelId) ? "local/model" : running.ModelId;
            ConnectionHint = $"✓ Подключено к запущенной модели: {Model}  ({BaseUrl}). Агент будет отвечать через неё.";
        }
        else
        {
            ConnectionHint = "Модель не запущена. Нажмите «🚀 Старт» (или «Только сервер») ниже — адрес и название " +
                "подставятся автоматически. Либо впишите онлайн-адрес вручную.";
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

    /// <summary>Единая кнопка «Старт»: при необходимости поднимает сервер модели и запускает агента.</summary>
    [RelayCommand]
    private async Task PrepareAndLaunchAsync()
    {
        if (string.IsNullOrWhiteSpace(ProjectFolder))
        {
            Status = "Выберите папку проекта.";
            return;
        }

        var isLocalUrl = BaseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        // Для локального сервера id модели обязан быть в формате local/<имя>.
        if (isLocalUrl && !Model.Trim().StartsWith("local/", StringComparison.Ordinal))
        {
            Model = RunningModel.Instance.IsRunning && RunningModel.Instance.ModelId.StartsWith("local/", StringComparison.Ordinal)
                ? RunningModel.Instance.ModelId
                : "local/model";
        }

        try
        {
            // Конфиг пишем сразу — он пригодится даже если агент не установлен.
            var request = new AgentLaunchRequest(SelectedAgent, ProjectFolder.Trim(), Model.Trim(), BaseUrl.Trim());
            var config = await new AgentProjectConfigWriter().WriteAsync(request, CancellationToken.None);

            // Локальный адрес, но сервер не поднят — стартуем его автоматически.
            if (isLocalUrl && !IsModelRunning)
            {
                if (string.IsNullOrWhiteSpace(LocalModelPath))
                {
                    Status = $"Конфиг записан ({config.Message}), но модель не выбрана — без неё агенту не к чему подключиться. " +
                        "Выберите модель выше (или впишите онлайн-адрес).";
                    return;
                }

                Status = "Запускаю сервер модели…";
                await StartServerAsync();
                SyncFromRunningModel();
                if (!IsModelRunning)
                {
                    Status = "Не удалось запустить модель: " + ServerStatus + " Агент не запущен.";
                    return;
                }

                // Адрес/модель обновились — перезапишем конфиг под запущенный сервер.
                request = new AgentLaunchRequest(SelectedAgent, ProjectFolder.Trim(), Model.Trim(), BaseUrl.Trim());
                config = await new AgentProjectConfigWriter().WriteAsync(request, CancellationToken.None);
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
                SaveLastLaunch();
                Status = $"Конфиг: {config.Message} Запущен агент «{SelectedAgent}» в новом окне.";
            }
            catch (Exception)
            {
                Status = $"Конфиг записан ({config.Message}), но агент «{SelectedAgent}» не найден в PATH. " +
                         "Установите его CLI и повторите. Команда: " + plan.Executable + " " + string.Join(' ', plan.Arguments);
            }
        }
        catch (ArgumentException)
        {
            Status = "Не удалось подготовить запуск агента: некорректный id модели. " +
                "Для локальной модели он должен быть в формате local/<имя>. Запустите модель и повторите.";
        }
        catch (Exception ex)
        {
            Status = "Ошибка подготовки запуска: " + ex.Message;
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
