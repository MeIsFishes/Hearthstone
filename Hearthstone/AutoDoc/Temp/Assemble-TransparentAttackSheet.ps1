param(
    [Parameter(Mandatory = $true)]
    [string]$Output,

    [Parameter(Mandatory = $true)]
    [string[]]$Inputs
)

$ErrorActionPreference = 'Stop'

if ($Inputs.Count -ne 8) {
    throw "Exactly 8 input frames are required; got $($Inputs.Count)."
}

Add-Type -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class TransparentAttackSheetAssembler
{
    private static Rectangle FindVisibleBounds(Bitmap bitmap)
    {
        var left = bitmap.Width;
        var top = bitmap.Height;
        var right = -1;
        var bottom = -1;

        for (var y = 0; y < bitmap.Height; y += 2)
        {
            for (var x = 0; x < bitmap.Width; x += 2)
            {
                if (bitmap.GetPixel(x, y).A <= 16)
                {
                    continue;
                }

                left = Math.Min(left, x);
                top = Math.Min(top, y);
                right = Math.Max(right, x);
                bottom = Math.Max(bottom, y);
            }
        }

        if (right < left)
        {
            return new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        }

        left = Math.Max(0, left - 12);
        top = Math.Max(0, top - 12);
        right = Math.Min(bitmap.Width - 1, right + 12);
        bottom = Math.Min(bitmap.Height - 1, bottom + 12);
        return Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    public static void Build(string[] inputs, string output)
    {
        using (var sheet = new Bitmap(1536, 1024, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(sheet))
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            for (var index = 0; index < inputs.Length; index++)
            {
                using (var frame = new Bitmap(inputs[index]))
                {
                    var source = FindVisibleBounds(frame);
                    var scale = Math.Min(360.0 / source.Width, 460.0 / source.Height);
                    var width = (int)Math.Round(source.Width * scale);
                    var height = (int)Math.Round(source.Height * scale);
                    var column = index % 4;
                    var row = index / 4;
                    var x = column * 384 + (384 - width) / 2;
                    var y = row * 512 + (512 - height) / 2;

                    graphics.DrawImage(
                        frame,
                        new Rectangle(x, y, width, height),
                        source,
                        GraphicsUnit.Pixel);
                }
            }

            sheet.Save(output, ImageFormat.Png);
        }
    }
}
'@ -ReferencedAssemblies System.Drawing

[TransparentAttackSheetAssembler]::Build($Inputs, $Output)
