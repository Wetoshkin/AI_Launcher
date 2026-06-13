using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Agents.Discovery;

namespace Launcher.Desktop.Services;

/// <summary>Устанавливает кодинг-агентов: npm/pip-пакеты с выводом в лог, либо открывает инструкцию.</summary>
public static class AgentInstaller
{
    public sealed record InstallResult(bool Started, bool Manual, string Message);

    /// <summary>
    /// Ставит агента. Для Manual-агентов открывает страницу с инструкцией.
    /// Для npm/pip запускает установку, стримит вывод в <paramref name="log"/> и ждёт завершения.
    /// </summary>
    public static async Task<InstallResult> InstallAsync(AgentInfo info, Action<string> log, CancellationToken ct)
    {
        if (info.Install == AgentInstallMethod.Manual || string.IsNullOrWhiteSpace(info.Package))
        {
            OpenDocs(info.DocsUrl);
            return new InstallResult(false, Manual: true,
                $"{info.Display} ставится вручную — открыл инструкцию в браузере. {info.Note}");
        }

        var command = info.Install switch
        {
            AgentInstallMethod.Npm => $"npm install -g {info.Package}",
            AgentInstallMethod.Pip => $"python -m pip install -U {info.Package}",
            _ => null,
        };

        if (command is null)
        {
            return new InstallResult(false, false, "Неизвестный способ установки.");
        }

        log($"$ {command}");

        var psi = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        Process process;
        try
        {
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => { if (e.Data is not null) log(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) log(e.Data); };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            var tool = info.Install == AgentInstallMethod.Npm ? "Node.js (npm)" : "Python (pip)";
            return new InstallResult(false, false,
                $"Не удалось запустить установку: {ex.Message}. Сначала установите {tool}.");
        }

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return new InstallResult(true, false, "Установка отменена.");
        }

        var ok = process.ExitCode == 0;
        return new InstallResult(true, false, ok
            ? $"{info.Display} установлен."
            : $"Установка {info.Display} завершилась с ошибкой (код {process.ExitCode}). Проверьте лог. " +
              (info.Install == AgentInstallMethod.Npm ? "Возможно, не установлен Node.js." : "Возможно, не установлен Python."));
    }

    private static void OpenDocs(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // браузер не открылся
        }
    }
}
