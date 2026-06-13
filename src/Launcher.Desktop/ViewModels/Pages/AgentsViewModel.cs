using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Agents.Commands;
using Launcher.Agents.Discovery;
using Launcher.Core.Scenarios;
using Launcher.Desktop.Services;

namespace Launcher.Desktop.ViewModels.Pages;

public sealed record AgentStatusRow(string Name, string Status, bool Installed);

public sealed partial class AgentsViewModel : ViewModelBase
{
    private readonly AgentCliCatalogService _catalog;

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
    private string _connectionHint = "Модель не запущена. Откройте вкладку «Чат» → «Локальный сервер» и запустите модель — адрес и название подставятся сюда автоматически.";

    public ObservableCollection<AgentStatusRow> Agents { get; } = new();

    public IReadOnlyList<AgentKind> AgentKinds { get; } =
        new[] { AgentKind.OpenCode, AgentKind.Kilo, AgentKind.Claw, AgentKind.Aider, AgentKind.Pi };

    public Func<string, Task<string?>>? PickFolderAsync { get; set; }

    public string Title => "Агенты";
    public string Description =>
        "Кодинг-агенты (OpenCode, Kilo и др.) работают с вашим проектом через локальную модель " +
        "или онлайн-провайдера. Сначала запустите модель (вкладка «Чат» → локальный сервер) или укажите онлайн-адрес.";

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

    /// <summary>Автоподстановка адреса и id модели из запущенного локального сервера.</summary>
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
            ConnectionHint = "Модель не запущена. Откройте вкладку «Чат» → «Локальный сервер» и запустите модель — " +
                "адрес и название подставятся сюда автоматически. Либо впишите онлайн-адрес и ключ вручную.";
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

            // Конфиг записан; но если адрес локальный, а сервер не запущен — запускать агента бессмысленно.
            var isLocalUrl = BaseUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);
            if (isLocalUrl && !IsModelRunning)
            {
                Status = $"Конфиг записан ({config.Message}), но локальная модель не запущена — агенту не к чему подключиться. " +
                    "Откройте «Чат» → «Локальный сервер», запустите модель и вернитесь сюда (адрес подставится сам).";
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
