# Generates a unique multi-size .ico for AI Launcher Studio: a white "launch spark" (lightning
# bolt) on a warm orange rounded square. ASCII-only.
param(
  [string]$Out = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\src\Launcher.Desktop\Assets\ai-launcher-studio.ico",
  [string]$PreviewPng = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\docs\images\app-icon.png"
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$s) {
  $bmp = New-Object System.Drawing.Bitmap $s, $s
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.Clear([System.Drawing.Color]::Transparent)

  # rounded square background with vertical gradient
  $pad = [Math]::Max(1, [int]($s * 0.06))
  $rectF = New-Object System.Drawing.RectangleF($pad, $pad, ($s - 2*$pad), ($s - 2*$pad))
  $radius = $s * 0.22
  $path = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = $radius * 2
  $path.AddArc($rectF.X, $rectF.Y, $d, $d, 180, 90)
  $path.AddArc($rectF.Right - $d, $rectF.Y, $d, $d, 270, 90)
  $path.AddArc($rectF.Right - $d, $rectF.Bottom - $d, $d, $d, 0, 90)
  $path.AddArc($rectF.X, $rectF.Bottom - $d, $d, $d, 90, 90)
  $path.CloseFigure()

  $c1 = [System.Drawing.Color]::FromArgb(255, 245, 140, 35)
  $c2 = [System.Drawing.Color]::FromArgb(255, 196, 90, 0)
  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rectF, $c1, $c2, 90)
  $g.FillPath($brush, $path)

  # white lightning bolt (normalized points scaled to size)
  $pts = @(
    @(0.56,0.10),@(0.30,0.54),@(0.47,0.54),@(0.40,0.90),@(0.72,0.44),@(0.54,0.44),@(0.62,0.10)
  )
  $poly = @()
  foreach ($pt in $pts) { $poly += New-Object System.Drawing.PointF(($pt[0]*$s), ($pt[1]*$s)) }
  $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255,255,255,255))
  $g.FillPolygon($white, [System.Drawing.PointF[]]$poly)

  $g.Dispose()
  return $bmp
}

$sizes = @(16,32,48,64,128,256)
$pngs = @()
foreach ($s in $sizes) {
  $bmp = New-IconBitmap $s
  $ms = New-Object System.IO.MemoryStream
  $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  $pngs += ,@($s, $ms.ToArray())
  if ($s -eq 256) {
    New-Item -ItemType Directory -Force -Path (Split-Path $PreviewPng) | Out-Null
    [System.IO.File]::WriteAllBytes($PreviewPng, $ms.ToArray())
  }
  $bmp.Dispose()
}

# assemble ICO with PNG-compressed entries
$fs = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)      # reserved
$bw.Write([UInt16]1)      # type icon
$bw.Write([UInt16]$pngs.Count)
$offset = 6 + 16 * $pngs.Count
foreach ($e in $pngs) {
  $size = $e[0]; $bytes = $e[1]
  $bw.Write([Byte]($(if ($size -ge 256) {0} else {$size})))  # width
  $bw.Write([Byte]($(if ($size -ge 256) {0} else {$size})))  # height
  $bw.Write([Byte]0)       # colors
  $bw.Write([Byte]0)       # reserved
  $bw.Write([UInt16]1)     # planes
  $bw.Write([UInt16]32)    # bpp
  $bw.Write([UInt32]$bytes.Length)
  $bw.Write([UInt32]$offset)
  $offset += $bytes.Length
}
foreach ($e in $pngs) { $bw.Write($e[1]) }
$bw.Flush()
New-Item -ItemType Directory -Force -Path (Split-Path $Out) | Out-Null
[System.IO.File]::WriteAllBytes($Out, $fs.ToArray())
$bw.Dispose(); $fs.Dispose()
Write-Output ("ICON " + $Out + " sizes " + ($sizes -join ","))
