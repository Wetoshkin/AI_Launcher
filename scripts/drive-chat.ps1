# Live end-to-end check of the chat. ASCII-only (PS 5.1 mis-decodes UTF-8 Cyrillic in .ps1).
# Caller must set $env:ALS_START_PAGE to the Chat page name before invoking.
param(
  [string]$Exe = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\src\Launcher.Desktop\bin\Debug\net8.0\Launcher.Desktop.exe",
  [string]$Out = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\TestResults\phase4-chat-live.png"
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Dpi { [DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); }
"@
[Dpi]::SetProcessDPIAware() | Out-Null

$p = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds 9
$p.Refresh()
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
if (-not $root) { Write-Output "NO_ROOT"; Stop-Process -Id $p.Id -Force; exit 1 }
$scope = [System.Windows.Automation.TreeScope]::Descendants

function AllOfType($ct) {
  $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ct)
  return $root.FindAll($scope, $cond)
}

$edits = AllOfType ([System.Windows.Automation.ControlType]::Edit)
$box = $null
foreach ($e in $edits) { if ($e.Current.Name -eq "ChatInput") { $box = $e } }
if (-not $box) { Write-Output ("NO_INPUT edits=" + $edits.Count); Stop-Process -Id $p.Id -Force; exit 1 }
$box.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue("Privet, kto ty?")
Start-Sleep -Milliseconds 900

# Send = first button with empty AutomationId (order: Send, Stop, Clear).
$send = $null
foreach ($b in (AllOfType ([System.Windows.Automation.ControlType]::Button))) {
  if ($b.Current.AutomationId -eq "" -and -not $send) { $send = $b }
}
if (-not $send) { Write-Output "NO_SEND"; Stop-Process -Id $p.Id -Force; exit 1 }
Write-Output ("send enabled=" + $send.Current.IsEnabled)
$send.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Output "SENT"
Start-Sleep -Seconds 4

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W3 { [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r); public struct R { public int L,T,Rr,B; } }
"@
$r = New-Object W3+R
[W3]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$w = $r.Rr - $r.L; $h = $r.B - $r.T
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output ("SAVED " + $Out)
Stop-Process -Id $p.Id -Force
