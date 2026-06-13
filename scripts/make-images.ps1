# Generates the app icon (refined, macOS-style rounded square + lightning) and a hero banner.
# ASCII-only.
param(
  [string]$AssetsDir = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\src\Launcher.Desktop\Assets",
  [string]$DocsImg = "D:\Nextcloud\Ai\Projects\AI Laucher\repo\docs\images"
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

function New-RoundedPath([System.Drawing.RectangleF]$r, [single]$radius) {
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = $radius * 2
  $p.AddArc($r.X, $r.Y, $d, $d, 180, 90)
  $p.AddArc($r.Right - $d, $r.Y, $d, $d, 270, 90)
  $p.AddArc($r.Right - $d, $r.Bottom - $d, $d, $d, 0, 90)
  $p.AddArc($r.X, $r.Bottom - $d, $d, $d, 90, 90)
  $p.CloseFigure()
  return $p
}

function Draw-Icon([int]$s) {
  $bmp = New-Object System.Drawing.Bitmap $s, $s
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $g.Clear([System.Drawing.Color]::Transparent)

  $pad = [single]($s * 0.045)
  $rect = New-Object System.Drawing.RectangleF($pad, $pad, ($s - 2*$pad), ($s - 2*$pad))
  $radius = $s * 0.235  # macOS-like rounding
  $path = New-RoundedPath $rect $radius

  # diagonal gradient: warm amber, lighter top-left to deeper bottom-right
  $c1 = [System.Drawing.Color]::FromArgb(255, 255, 158, 64)
  $c2 = [System.Drawing.Color]::FromArgb(255, 226, 96, 8)
  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 55)
  $g.FillPath($brush, $path)

  # subtle top highlight (skip on tiny sizes)
  if ($s -ge 48) {
    $hrf = New-Object System.Drawing.RectangleF($rect.X, $rect.Y, $rect.Width, [single]($rect.Height*0.5))
    $hi = New-Object System.Drawing.Drawing2D.LinearGradientBrush($hrf,
      [System.Drawing.Color]::FromArgb(70, 255, 255, 255),
      [System.Drawing.Color]::FromArgb(0, 255, 255, 255), 90)
    $g.SetClip($path)
    $g.FillRectangle($hi, $rect.X, $rect.Y, $rect.Width, [single]($rect.Height*0.5))
    $g.ResetClip()
  }

  # lightning bolt (clean, centered), with soft shadow
  $pts = @(0.575,0.135, 0.34,0.545, 0.49,0.545, 0.42,0.865, 0.69,0.43, 0.535,0.43, 0.605,0.135)
  $poly = @()
  for ($i=0; $i -lt $pts.Length; $i+=2) { $poly += New-Object System.Drawing.PointF(($pts[$i]*$s), ($pts[$i+1]*$s)) }
  $shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(45,0,0,0))
  $g.TranslateTransform(0, [single]($s*0.012))
  $g.FillPolygon($shadow, [System.Drawing.PointF[]]$poly)
  $g.ResetTransform()
  $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
  $g.FillPolygon($white, [System.Drawing.PointF[]]$poly)

  $g.Dispose()
  return $bmp
}

# --- icon: png + multi-size ico ---
New-Item -ItemType Directory -Force -Path $AssetsDir | Out-Null
New-Item -ItemType Directory -Force -Path $DocsImg | Out-Null
$sizes = @(16,32,48,64,128,256)
$pngs = @()
foreach ($s in $sizes) {
  $bmp = Draw-Icon $s
  $ms = New-Object System.IO.MemoryStream
  $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
  $pngs += ,@($s, $ms.ToArray())
  if ($s -eq 256) {
    [System.IO.File]::WriteAllBytes((Join-Path $AssetsDir "ai-launcher-studio.png"), $ms.ToArray())
    [System.IO.File]::WriteAllBytes((Join-Path $DocsImg "app-icon.png"), $ms.ToArray())
  }
  $bmp.Dispose()
}
$fs = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$pngs.Count)
$offset = 6 + 16 * $pngs.Count
foreach ($e in $pngs) {
  $size=$e[0]; $bytes=$e[1]
  $bw.Write([Byte]($(if ($size -ge 256){0}else{$size}))); $bw.Write([Byte]($(if ($size -ge 256){0}else{$size})))
  $bw.Write([Byte]0); $bw.Write([Byte]0); $bw.Write([UInt16]1); $bw.Write([UInt16]32)
  $bw.Write([UInt32]$bytes.Length); $bw.Write([UInt32]$offset); $offset += $bytes.Length
}
foreach ($e in $pngs) { $bw.Write($e[1]) }
$bw.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $AssetsDir "ai-launcher-studio.ico"), $fs.ToArray())
$bw.Dispose(); $fs.Dispose()
Write-Output "icon done"

# --- hero banner ---
$hw = 1280; $hh = 300
$hero = New-Object System.Drawing.Bitmap $hw, $hh
$hg = [System.Drawing.Graphics]::FromImage($hero)
$hg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$hrect = New-Object System.Drawing.RectangleF(0,0,$hw,$hh)
$hb = New-Object System.Drawing.Drawing2D.LinearGradientBrush($hrect,
  [System.Drawing.Color]::FromArgb(255, 245, 130, 35),
  [System.Drawing.Color]::FromArgb(255, 224, 86, 8), 20)
$hg.FillRectangle($hb, $hrect)
# soft bokeh circles
$rng = New-Object System.Random 7
foreach ($i in 0..9) {
  $cr = $rng.Next(40,160)
  $cx = $rng.Next(0,$hw); $cy = $rng.Next(-40,$hh)
  $alpha = $rng.Next(10,34)
  $cb = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb($alpha,255,255,255))
  $hg.FillEllipse($cb, $cx-$cr, $cy-$cr, $cr*2, $cr*2)
}
# big translucent bolt on the right
$bs = $hh * 1.4
$ox = $hw - $bs*0.62; $oy = -$hh*0.18
$pts2 = @(0.575,0.135, 0.34,0.545, 0.49,0.545, 0.42,0.865, 0.69,0.43, 0.535,0.43, 0.605,0.135)
$poly2 = @()
for ($i=0; $i -lt $pts2.Length; $i+=2) { $poly2 += New-Object System.Drawing.PointF(($ox + $pts2[$i]*$bs), ($oy + $pts2[$i+1]*$bs)) }
$wb = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(38,255,255,255))
$hg.FillPolygon($wb, [System.Drawing.PointF[]]$poly2)
$hero.Save((Join-Path $AssetsDir "hero.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$hg.Dispose(); $hero.Dispose()
Write-Output "hero done"
