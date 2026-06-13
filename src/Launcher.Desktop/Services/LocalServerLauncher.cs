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
        CancellationToken cancellationToken,
        bool antiLoop = true,
        string? tensorSplit = null,
        string? extraArgs = null)
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
        var args = new System.Collections.Generic.List<string>
        {
            "-m", modelPath,
            "--alias", modelId,
            "--ctx-size", contextTokens.ToString(),
            "--port", port.ToString(),
            "--host", "127.0.0.1",
        };
        if (antiLoop)
        {
            // DRY — лучший безопасный приём против зацикливания (см. ресёрч).
            args.Add("--dry-multiplier");
            args.Add("0.8");
        }
        if (!string.IsNullOrWhiteSpace(tensorSplit))
        {
            // Распределение по нескольким видеокартам пропорционально VRAM.
            args.Add("--tensor-split");
            args.Add(tensorSplit!);
        }
        if (!string.IsNullOrWhiteSpace(extraArgs))
        {
            // Режим Эксперт: произвольные дополнительные флаги llama-server.
            foreach (var token in SplitArgs(extraArgs!))
            {
                args.Add(token);
            }
        }

        foreach (var arg in args)
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

    /// <summary>Разбивает строку аргументов на токены с учётом кавычек.</summary>
    public static System.Collections.Generic.IReadOnlyList<string> SplitArgs(string input)
    {
        var result = new System.Collections.Generic.List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;
        foreach (var ch in input)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(ch) && !inQuotes)
            {
                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(ch);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
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

        // Подчищаем «осиротевшие» серверы инференса (например, после перезапуска приложения).
        KillStrayInferenceServers();
    }

    /// <summary>
    /// Убивает оставшиеся процессы локального инференса по имени — на случай, если они
    /// не были отслежены (повторный запуск, падение приложения и т.п.).
    /// </summary>
    public static void KillStrayInferenceServers()
    {
        // ollama сознательно не трогаем: это отдельная служба, которую приложение не запускает.
        string[] names = { "llama-server", "llama-cli", "llama-bench" };
        foreach (var name in names)
        {
            Process[] procs;
            try
            {
                procs = Process.GetProcessesByName(name);
            }
            catch
            {
                continue;
            }

            foreach (var proc in procs)
            {
                try
                {
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(3000);
                }
                catch
                {
                    // нет прав/уже завершён
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }
    }
}
