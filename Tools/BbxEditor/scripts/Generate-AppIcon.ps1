param(
    [string]$OutputPath = (Join-Path $PSScriptRoot '..\src\BbxEditor.Wpf\Assets\BbxEditor.ico')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF]$Bounds,
        [float]$Radius
    )

    $diameter = $Radius * 2
    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $path.AddArc($Bounds.Left, $Bounds.Top, $diameter, $diameter, 180, 90)
    $path.AddArc($Bounds.Right - $diameter, $Bounds.Top, $diameter, $diameter, 270, 90)
    $path.AddArc($Bounds.Right - $diameter, $Bounds.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($Bounds.Left, $Bounds.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPngBytes {
    param([int]$Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $backgroundBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 9, 38, 76))
    $whiteBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $xPen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [Math]::Max(1.4, $Size * 0.075))
    $font = [System.Drawing.Font]::new('Arial', [Math]::Max(7, $Size * 0.50), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $format = [System.Drawing.StringFormat]::new()
    $stream = [System.IO.MemoryStream]::new()
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

        $margin = [Math]::Max(0.5, $Size * 0.025)
        $bounds = [System.Drawing.RectangleF]::new($margin, $margin, $Size - $margin * 2, $Size - $margin * 2)
        $backgroundPath = New-RoundedRectanglePath -Bounds $bounds -Radius ($Size * 0.14)
        try { $graphics.FillPath($backgroundBrush, $backgroundPath) } finally { $backgroundPath.Dispose() }

        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        $textBounds = [System.Drawing.RectangleF]::new(0, $Size * 0.075, $Size, $Size * 0.55)
        $graphics.DrawString('BB', $font, $whiteBrush, $textBounds, $format)

        $xPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $xPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawLine($xPen, $Size * 0.20, $Size * 0.60, $Size * 0.80, $Size * 0.82)
        $graphics.DrawLine($xPen, $Size * 0.80, $Size * 0.60, $Size * 0.20, $Size * 0.82)

        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return $stream.ToArray()
    }
    finally {
        $stream.Dispose()
        $format.Dispose()
        $font.Dispose()
        $xPen.Dispose()
        $whiteBrush.Dispose()
        $backgroundBrush.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$iconSizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$iconFrames = @($iconSizes | ForEach-Object {
    [pscustomobject]@{ Size = $_; Bytes = (New-IconPngBytes -Size $_) }
})

$outputDirectory = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$iconStream = [System.IO.MemoryStream]::new()
$writer = [System.IO.BinaryWriter]::new($iconStream)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$iconFrames.Count)
    $dataOffset = 6 + 16 * $iconFrames.Count
    foreach ($frame in $iconFrames) {
        $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$frame.Bytes.Length)
        $writer.Write([uint32]$dataOffset)
        $dataOffset += $frame.Bytes.Length
    }
    foreach ($frame in $iconFrames) { $writer.Write([byte[]]$frame.Bytes) }
    $writer.Flush()
    [System.IO.File]::WriteAllBytes([System.IO.Path]::GetFullPath($OutputPath), $iconStream.ToArray())
}
finally {
    $writer.Dispose()
    $iconStream.Dispose()
}

Write-Output "Generated icon: $([System.IO.Path]::GetFullPath($OutputPath))"
