using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Runtimes.Startup;

namespace Launcher.Desktop.Services;

public sealed record LocalServerStartResult(bool Ready, string BaseUrl, string Model, string Message);

/// <summary>
/// Запускает локальный llama-server с выбранной моделью и ждёт готовности endpoint
/// (GET /v1/models). Управляет жизненным циклом процесса (старт/стоп).
/// </summary>
public sealed class LocalServerLauncher
{
    private Process? _process;

    public bool IsRunning => _process is { HasExited: false };

    /// <summary>Находит самый свежий установленный llama-server.exe в кэше приложения.</summary>
    public static string? FindInstalledRuntime()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AILauncherStudio", "runtimes");
        if (!Directory.Exists(root))
        {
            return null;
        }

        return Directory.EnumerateFiles(root, "llama-server.exe", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    public async Task<LocalServerStartResult> StartAsync(
        string runtimeExe,
        string modelPath,
        int port,
        int contextTokens,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        Stop();

        if (!File.Exists(runtimeExe))
        {
            return new LocalServerStartResult(false, "", "", "Не найден llama-server.exe. Установите runtime на вкладке «Среды».");
        }

        if (!File.Exists(modelPath))
        {
            return new LocalServerStartResult(false, "", "", "Не найден файл модели. Выберите .gguf или скачайте модель.");
        }

        var modelId = "local/" + Path.GetFileNameWithoutExtension(modelPath);
        var psi = new ProcessStartInfo(runtimeExe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(runtimeExe),
        };
        foreach (var arg in new[]
                 {
                     "-m", modelPath,
                     "--alias", modelId,
                     "--ctx-size", contextTokens.ToString(),
                     "--port", port.ToString(),
                     "--host", "127.0.0.1",
                 })
        {
            psi.ArgumentList.Add(arg);
        }

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (log is not null)
        {
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) log(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) log(e.Data); };
        }

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _process = process;

        var baseUrl = $"http://127.0.0.1:{port}/v1";
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var health = new OpenAiEndpointHealthClient(http);
        var ready = await health.WaitUntilReadyAsync(baseUrl, 90, TimeSpan.FromSeconds(1), cancellationToken);

        if (process.HasExited)
        {
            return new LocalServerStartResult(false, baseUrl, modelId,
                "Сервер завершился при запуске. Проверьте лог: возможно, модель не подходит или мало памяти.");
        }

        return new LocalServerStartResult(ready.IsReady, baseUrl, modelId,
            ready.IsReady ? "Сервер готов." : "Сервер не ответил вовремя: " + ready.Message);
    }

    public void Stop()
    {
        if (_process is { HasExited: false } p)
        {
            try
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(5000);
            }
            catch
            {
                // процесс уже мог завершиться
            }
        }

        _process?.Dispose();
        _process = null;
    }
}
