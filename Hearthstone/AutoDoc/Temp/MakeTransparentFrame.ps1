param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::FromFile($InputPath)
$result = New-Object System.Drawing.Bitmap($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$fitted = $null

try {
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            $pixel = $source.GetPixel($x, $y)
            $max = [Math]::Max($pixel.R, [Math]::Max($pixel.G, $pixel.B))
            $min = [Math]::Min($pixel.R, [Math]::Min($pixel.G, $pixel.B))
            $chroma = $max - $min
            $luma = (0.2126 * $pixel.R) + (0.7152 * $pixel.G) + (0.0722 * $pixel.B)

            $chromaAlpha = [Math]::Max(0.0, [Math]::Min(1.0, ($chroma - 8.0) / 24.0))
            $darkAlpha = [Math]::Max(0.0, [Math]::Min(1.0, (215.0 - $luma) / 25.0))
            $alpha = [int][Math]::Round(255.0 * [Math]::Max($chromaAlpha, $darkAlpha))

            $result.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, $pixel.R, $pixel.G, $pixel.B))
        }
    }

    $minX = $result.Width
    $minY = $result.Height
    $maxX = -1
    $maxY = -1
    for ($y = 0; $y -lt $result.Height; $y++) {
        for ($x = 0; $x -lt $result.Width; $x++) {
            if ($result.GetPixel($x, $y).A -gt 16) {
                $minX = [Math]::Min($minX, $x)
                $minY = [Math]::Min($minY, $y)
                $maxX = [Math]::Max($maxX, $x)
                $maxY = [Math]::Max($maxY, $y)
            }
        }
    }

    $sourcePadding = 6
    $minX = [Math]::Max(0, $minX - $sourcePadding)
    $minY = [Math]::Max(0, $minY - $sourcePadding)
    $maxX = [Math]::Min($result.Width - 1, $maxX + $sourcePadding)
    $maxY = [Math]::Min($result.Height - 1, $maxY + $sourcePadding)

    $crop = [System.Drawing.Rectangle]::new([int]$minX, [int]$minY, [int]($maxX - $minX + 1), [int]($maxY - $minY + 1))
    $destination = [System.Drawing.Rectangle]::new(8, 8, [int]($source.Width - 16), [int]($source.Height - 16))
    $fitted = New-Object System.Drawing.Bitmap($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($fitted)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($result, $destination, $crop, [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally {
        $graphics.Dispose()
    }

    $fitted.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    if ($null -ne $fitted) {
        $fitted.Dispose()
    }
    $result.Dispose()
    $source.Dispose()
}
