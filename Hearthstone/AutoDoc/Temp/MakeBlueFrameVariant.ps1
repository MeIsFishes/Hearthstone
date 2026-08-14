param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::FromFile($InputPath)
$result = New-Object System.Drawing.Bitmap($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

try {
    for ($y = 0; $y -lt $source.Height; $y++) {
        for ($x = 0; $x -lt $source.Width; $x++) {
            $pixel = $source.GetPixel($x, $y)
            if ($pixel.A -eq 0) {
                $result.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
                continue
            }

            $redLacquer = $pixel.R -gt 35 -and $pixel.G -lt ($pixel.R * 0.5) -and $pixel.B -lt ($pixel.R * 0.45)
            if ($redLacquer) {
                $blueR = [int][Math]::Min(255, [Math]::Round(($pixel.B * 0.55) + ($pixel.R * 0.03)))
                $blueG = [int][Math]::Min(255, [Math]::Round(($pixel.G * 0.8) + ($pixel.R * 0.12)))
                $blueB = [int][Math]::Min(255, [Math]::Round(($pixel.R * 0.95) + ($pixel.B * 0.25)))
                $result.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($pixel.A, $blueR, $blueG, $blueB))
            }
            else {
                $result.SetPixel($x, $y, $pixel)
            }
        }
    }

    $result.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $result.Dispose()
    $source.Dispose()
}
