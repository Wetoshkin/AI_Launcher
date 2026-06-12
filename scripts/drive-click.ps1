# Generic UIAutomation driver: opens a page, optionally sets an edit, invokes a named button, screenshots.
# Cyrillic comes via parameters (passed at call time, correctly encoded) — never as .ps1 literals.
param(
  [Parameter(Mandatory=$true)][string]$Page,
  [Parameter(Mandatory=$true)][string]$Button,
  [string]$EditText = "",
  [int]$EditIndex = -1,
  [string]$Out = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\TestResults\drive.png",
  [int]$WaitSeconds = 5,
  [string]$Exe = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\src\Launcher.Desktop\bin\Debug\net8.0\Launcher.Desktop.exe"
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

$env:ALS_START_PAGE = $Page
$p = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds 9
$p.Refresh()
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$scope = [System.Windows.Automation.TreeScope]::Descendants
function AllOfType($ct) {
  $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ct)
  return $root.FindAll($scope, $cond)
}

if ($EditIndex -ge 0 -and $EditText -ne "") {
  $edits = AllOfType ([System.Windows.Automation.ControlType]::Edit)
  if ($EditIndex -lt $edits.Count) {
    $edits[$EditIndex].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($EditText)
    Start-Sleep -Milliseconds 600
  }
}

$btn = $null
foreach ($b in (AllOfType ([System.Windows.Automation.ControlType]::Button))) { if ($b.Current.Name -eq $Button) { $btn = $b } }
if (-not $btn) { Write-Output "NO_BUTTON"; Stop-Process -Id $p.Id -Force; exit 1 }
$btn.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Output "CLICKED"
Start-Sleep -Seconds $WaitSeconds

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
