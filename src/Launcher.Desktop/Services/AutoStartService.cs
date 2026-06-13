using System;
using System.Diagnostics;

namespace Launcher.Desktop.Services;

/// <summary>Запуск приложения вместе с Windows (ключ автозапуска HKCU\...\Run). Через reg.exe, без админ-прав.</summary>
public static class AutoStartService
{
    private const string RunKey = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "AILauncherStudio";

    public static bool IsEnabled()
    {
        try
        {
            var output = Run($"query \"{RunKey}\" /v {ValueName}");
            return output.Contains(ValueName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void SetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(exe))
                {
                    return;
                }

                Run($"add \"{RunKey}\" /v {ValueName} /t REG_SZ /d \"\\\"{exe}\\\"\" /f");
            }
            else
            {
                Run($"delete \"{RunKey}\" /v {ValueName} /f");
            }
        }
        catch
        {
            // не критично
        }
    }

    private static string Run(string arguments)
    {
        var psi = new ProcessStartInfo("reg.exe", arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(4000);
        return output;
    }
}
