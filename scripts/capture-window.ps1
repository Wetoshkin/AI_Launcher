# Launches an executable, waits, captures its main window to a PNG, then closes it.
# ASCII-only on purpose (PowerShell here mis-decodes UTF-8 Cyrillic in scripts).
param(
    [Parameter(Mandatory=$true)][string]$Exe,
    [Parameter(Mandatory=$true)][string]$Out,
    [int]$DelaySeconds = 8,
    [switch]$Maximize
)

$ErrorActionPreference = "Stop"

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

[Win]::SetProcessDPIAware() | Out-Null

New-Item -ItemType Directory -Force -Path (Split-Path $Out) | Out-Null
$p = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds $DelaySeconds
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Output "NO_WINDOW_HANDLE"; Stop-Process -Id $p.Id -Force; exit 1 }

if ($Maximize) { [Win]::ShowWindow($h, 3) | Out-Null } else { [Win]::ShowWindow($h, 9) | Out-Null }
[Win]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 1000

$r = New-Object Win+RECT
[Win]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.Right - $r.Left
$hgt = $r.Bottom - $r.Top
Write-Output ("WINDOW {0},{1} {2}x{3}" -f $r.Left, $r.Top, $w, $hgt)

Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap $w, $hgt
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output ("SAVED " + $Out)
Stop-Process -Id $p.Id -Force
