using System.Text;
using Launcher.Runtimes.Ports;

namespace Launcher.Runtimes.Hardware;

/// <summary>
/// Определяет железо на Windows через PowerShell/CIM (работает для Intel/AMD/NVIDIA и iGPU,
/// в отличие от probe только под nvidia-smi). Для дискретных карт берёт точный объём VRAM
/// из реестра (qwMemorySize), т.к. AdapterRAM ограничен 4 ГБ.
/// Скрипт передаётся через -EncodedCommand (Base64/UTF-16LE), чтобы вложенные кавычки
/// не ломали разбор командной строки.
/// </summary>
public sealed class WmiHardwareProbe(ICommandRunner commandRunner) : IHardwareProbe
{
    // Эмитит нормализованные строки CPU|.. / GPU|name|bytes / RAM|total|free.
    private const string Script = """
        $ErrorActionPreference='SilentlyContinue';
        $cpu=(Get-CimInstance Win32_Processor | Select-Object -First 1).Name;
        Write-Output ("CPU|"+$cpu);
        $cls='HKLM:\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}';
        $regs=Get-ChildItem $cls | ForEach-Object { Get-ItemProperty $_.PSPath };
        foreach ($g in Get-CimInstance Win32_VideoController) {
          $mem=[int64]($g.AdapterRAM);
          foreach ($r in $regs) {
            if ($r.DriverDesc -eq $g.Name -and $r.'HardwareInformation.qwMemorySize') {
              $q=[int64]$r.'HardwareInformation.qwMemorySize'; if ($q -gt $mem) { $mem=$q } } }
          Write-Output ("GPU|"+$g.Name+"|"+$mem) };
        $cs=Get-CimInstance Win32_ComputerSystem;
        $os=Get-CimInstance Win32_OperatingSystem;
        Write-Output ("RAM|"+[int64]$cs.TotalPhysicalMemory+"|"+([int64]$os.FreePhysicalMemory*1024))
        """;

    public async Task<SystemHardware> GetHardwareAsync(CancellationToken cancellationToken)
    {
        try
        {
            var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(Script));
            var arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}";
            var output = await commandRunner.RunAsync("powershell", arguments, cancellationToken);
            return HardwareProbeParser.Parse(output);
        }
        catch
        {
            return SystemHardware.Empty;
        }
    }
}
