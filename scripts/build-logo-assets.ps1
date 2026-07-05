[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent $PSScriptRoot
$assetsDirectory = Join-Path $projectRoot 'src\Horizon.App\Assets'
$masterPath = Join-Path $assetsDirectory 'horizon-logo-master.png'
$pngPath = Join-Path $assetsDirectory 'horizon-logo.png'
$icoPath = Join-Path $assetsDirectory 'Horizon.ico'
$iconSizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF]$Rectangle,
        [float]$Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $path.AddArc($Rectangle.Left, $Rectangle.Top, $diameter, $diameter, 180, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Top, $diameter, $diameter, 270, 90)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Rectangle.Left, $Rectangle.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function Get-InterpolatedColor {
    param(
        [System.Drawing.Color]$From,
        [System.Drawing.Color]$To,
        [double]$Amount,
        [int]$Alpha = 255
    )

    $red = [int][Math]::Round($From.R + (($To.R - $From.R) * $Amount))
    $green = [int][Math]::Round($From.G + (($To.G - $From.G) * $Amount))
    $blue = [int][Math]::Round($From.B + (($To.B - $From.B) * $Amount))
    return [System.Drawing.Color]::FromArgb($Alpha, $red, $green, $blue)
}

function Get-OrbitColor {
    param(
        [double]$Amount,
        [int]$Alpha = 255
    )

    $cyan = [System.Drawing.ColorTranslator]::FromHtml('#57F1FF')
    $blue = [System.Drawing.ColorTranslator]::FromHtml('#4E87FF')
    $violet = [System.Drawing.ColorTranslator]::FromHtml('#CB58FF')
    if ($Amount -le 0.5) {
        return Get-InterpolatedColor $cyan $blue ($Amount * 2) $Alpha
    }

    return Get-InterpolatedColor $blue $violet (($Amount - 0.5) * 2) $Alpha
}

function Draw-Orbit {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.RectangleF]$Bounds,
        [float]$StrokeWidth,
        [int]$Alpha
    )

    $startAngle = -42.0
    for ($index = 0; $index -lt 360; $index++) {
        $amount = $index / 359.0
        $pen = [System.Drawing.Pen]::new((Get-OrbitColor $amount $Alpha), $StrokeWidth)
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        try {
            $Graphics.DrawArc($pen, $Bounds, [float]($startAngle + $index), 1.8)
        }
        finally {
            $pen.Dispose()
        }
    }
}

function New-LogoBitmap {
    param([int]$Size)

    $isSmallFrame = $Size -le 32
    $supersample = if ($Size -le 32) { 8 } elseif ($Size -le 128) { 4 } else { 1 }
    $renderSize = $Size * $supersample
    $bitmap = [System.Drawing.Bitmap]::new(
        $renderSize,
        $renderSize,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

        $plateMargin = [float]($renderSize * $(if ($isSmallFrame) { 0.025 } else { 0.07 }))
        $plateBounds = [System.Drawing.RectangleF]::new(
            $plateMargin,
            $plateMargin,
            $renderSize - ($plateMargin * 2),
            $renderSize - ($plateMargin * 2))
        $platePath = New-RoundedRectanglePath $plateBounds ([float]($renderSize * $(if ($isSmallFrame) { 0.225 } else { 0.205 })))
        $plateBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            $plateBounds,
            [System.Drawing.ColorTranslator]::FromHtml('#13264B'),
            [System.Drawing.ColorTranslator]::FromHtml('#020711'),
            45.0)
        try {
            $graphics.FillPath($plateBrush, $platePath)
        }
        finally {
            $plateBrush.Dispose()
        }

        $borderPen = [System.Drawing.Pen]::new(
            [System.Drawing.Color]::FromArgb(150, 49, 85, 139),
            [float][Math]::Max(1, $renderSize * 0.01))
        try {
            $graphics.DrawPath($borderPen, $platePath)
        }
        finally {
            $borderPen.Dispose()
            $platePath.Dispose()
        }

        $orbitRadius = $renderSize * $(if ($isSmallFrame) { 0.325 } else { 0.275 })
        $orbitBounds = [System.Drawing.RectangleF]::new(
            [float](($renderSize / 2) - $orbitRadius),
            [float](($renderSize / 2) - $orbitRadius),
            [float]($orbitRadius * 2),
            [float]($orbitRadius * 2))
        $coreOrbitWidth = [float]($renderSize * $(if ($isSmallFrame) { 0.09 } else { 0.058 }))
        if ($Size -gt 32) {
            Draw-Orbit $graphics $orbitBounds ([float]($coreOrbitWidth * 2.25)) 28
            Draw-Orbit $graphics $orbitBounds ([float]($coreOrbitWidth * 1.45)) 62
        }
        Draw-Orbit $graphics $orbitBounds $coreOrbitWidth 255

        $checkStartX = if ($isSmallFrame) { 0.30 } else { 0.345 }
        $checkStartY = if ($isSmallFrame) { 0.52 } else { 0.515 }
        $checkMiddleX = if ($isSmallFrame) { 0.445 } else { 0.46 }
        $checkMiddleY = if ($isSmallFrame) { 0.665 } else { 0.63 }
        $checkEndX = if ($isSmallFrame) { 0.73 } else { 0.685 }
        $checkEndY = if ($isSmallFrame) { 0.33 } else { 0.365 }
        $checkPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
        $checkPath.AddLines(@(
            [System.Drawing.PointF]::new([float]($renderSize * $checkStartX), [float]($renderSize * $checkStartY)),
            [System.Drawing.PointF]::new([float]($renderSize * $checkMiddleX), [float]($renderSize * $checkMiddleY)),
            [System.Drawing.PointF]::new([float]($renderSize * $checkEndX), [float]($renderSize * $checkEndY))
        ))
        $checkWidth = [float]($renderSize * $(if ($isSmallFrame) { 0.105 } else { 0.074 }))
        if ($Size -gt 32) {
            $checkGlowPen = [System.Drawing.Pen]::new(
                [System.Drawing.Color]::FromArgb(72, 87, 225, 255),
                [float]($checkWidth * 1.55))
            $checkGlowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $checkGlowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $checkGlowPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
            try {
                $graphics.DrawPath($checkGlowPen, $checkPath)
            }
            finally {
                $checkGlowPen.Dispose()
            }
        }

        $checkPen = [System.Drawing.Pen]::new(
            [System.Drawing.ColorTranslator]::FromHtml('#D8FBFF'),
            $checkWidth)
        $checkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $checkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $checkPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        try {
            $graphics.DrawPath($checkPen, $checkPath)
        }
        finally {
            $checkPen.Dispose()
            $checkPath.Dispose()
        }

        $nodeRadius = $renderSize * $(if ($isSmallFrame) { 0.067 } else { 0.058 })
        $nodeCenterX = $renderSize * $(if ($isSmallFrame) { 0.73 } else { 0.695 })
        $nodeCenterY = $renderSize * $(if ($isSmallFrame) { 0.27 } else { 0.305 })
        if ($Size -gt 32) {
            $nodeGlowBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(65, 190, 73, 255))
            try {
                $graphics.FillEllipse(
                    $nodeGlowBrush,
                    [float]($nodeCenterX - ($nodeRadius * 1.65)),
                    [float]($nodeCenterY - ($nodeRadius * 1.65)),
                    [float]($nodeRadius * 3.3),
                    [float]($nodeRadius * 3.3))
            }
            finally {
                $nodeGlowBrush.Dispose()
            }
        }

        $nodeBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            [System.Drawing.RectangleF]::new(
                [float]($nodeCenterX - $nodeRadius),
                [float]($nodeCenterY - $nodeRadius),
                [float]($nodeRadius * 2),
                [float]($nodeRadius * 2)),
            [System.Drawing.ColorTranslator]::FromHtml('#E1A7FF'),
            [System.Drawing.ColorTranslator]::FromHtml('#7025D8'),
            55.0)
        try {
            $graphics.FillEllipse(
                $nodeBrush,
                [float]($nodeCenterX - $nodeRadius),
                [float]($nodeCenterY - $nodeRadius),
                [float]($nodeRadius * 2),
                [float]($nodeRadius * 2))
        }
        finally {
            $nodeBrush.Dispose()
        }

        if ($supersample -eq 1) {
            return $bitmap
        }

        $output = [System.Drawing.Bitmap]::new(
            $Size,
            $Size,
            [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $outputGraphics = [System.Drawing.Graphics]::FromImage($output)
        try {
            $outputGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $outputGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $outputGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $outputGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $outputGraphics.DrawImage($bitmap, 0, 0, $Size, $Size)
        }
        finally {
            $outputGraphics.Dispose()
            $bitmap.Dispose()
        }

        return $output
    }
    finally {
        $graphics.Dispose()
    }
}

function Save-Png {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [string]$Path
    )

    $Bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
}

function Resize-MasterIfNeeded {
    if (-not (Test-Path -LiteralPath $masterPath)) {
        throw "Visual master not found: $masterPath"
    }

    $source = [System.Drawing.Image]::FromFile($masterPath)
    try {
        if ($source.Width -eq 1024 -and $source.Height -eq 1024) {
            return
        }

        $resized = [System.Drawing.Bitmap]::new(1024, 1024, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [System.Drawing.Graphics]::FromImage($resized)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.DrawImage($source, 0, 0, 1024, 1024)
        }
        finally {
            $graphics.Dispose()
        }
    }
    finally {
        $source.Dispose()
    }

    $temporaryPath = "$masterPath.resize.png"
    try {
        $resized.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
        Copy-Item -LiteralPath $temporaryPath -Destination $masterPath -Force
    }
    finally {
        $resized.Dispose()
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

function Write-MultiSizeIcon {
    param(
        [int[]]$Sizes,
        [string]$Path
    )

    $frames = @()
    foreach ($size in $Sizes) {
        $bitmap = New-LogoBitmap $size
        $stream = [System.IO.MemoryStream]::new()
        try {
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $frames += [pscustomobject]@{ Size = $size; Bytes = $stream.ToArray() }
        }
        finally {
            $stream.Dispose()
            $bitmap.Dispose()
        }
    }

    $fileStream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create)
    $writer = [System.IO.BinaryWriter]::new($fileStream)
    try {
        $writer.Write([uint16]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]$frames.Count)
        $offset = 6 + (16 * $frames.Count)

        foreach ($frame in $frames) {
            $dimension = if ($frame.Size -ge 256) { 0 } else { $frame.Size }
            $writer.Write([byte]$dimension)
            $writer.Write([byte]$dimension)
            $writer.Write([byte]0)
            $writer.Write([byte]0)
            $writer.Write([uint16]1)
            $writer.Write([uint16]32)
            $writer.Write([uint32]$frame.Bytes.Length)
            $writer.Write([uint32]$offset)
            $offset += $frame.Bytes.Length
        }

        foreach ($frame in $frames) {
            $writer.Write([byte[]]$frame.Bytes)
        }
    }
    finally {
        $writer.Dispose()
        $fileStream.Dispose()
    }
}

New-Item -ItemType Directory -Path $assetsDirectory -Force | Out-Null
Resize-MasterIfNeeded

$mainBitmap = New-LogoBitmap 1024
try {
    Save-Png $mainBitmap $pngPath
}
finally {
    $mainBitmap.Dispose()
}

Write-MultiSizeIcon $iconSizes $icoPath

$png = [System.Drawing.Image]::FromFile($pngPath)
try {
    if ($png.Width -ne 1024 -or $png.Height -ne 1024) {
        throw "Unexpected PNG dimensions: $($png.Width)x$($png.Height)"
    }
}
finally {
    $png.Dispose()
}

$iconBytes = [System.IO.File]::ReadAllBytes($icoPath)
$frameCount = [BitConverter]::ToUInt16($iconBytes, 4)
if ([BitConverter]::ToUInt16($iconBytes, 2) -ne 1 -or $frameCount -ne $iconSizes.Count) {
    throw "Invalid ICO header or frame count: $frameCount"
}

Write-Host 'Horizon logo assets generated:'
Write-Host "  Master: $masterPath"
Write-Host "  PNG:    $pngPath ($((Get-Item $pngPath).Length) bytes)"
Write-Host "  ICO:    $icoPath ($frameCount frames, $((Get-Item $icoPath).Length) bytes)"
