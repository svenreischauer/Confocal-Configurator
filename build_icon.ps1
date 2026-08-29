param(
    [string]$SourcePath = (Join-Path $PSScriptRoot "app-icon-source.png"),
    [string]$PngPath = (Join-Path $PSScriptRoot "app-icon.png"),
    [string]$IconPath = (Join-Path $PSScriptRoot "app-icon.ico")
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath
{
    param(
        [System.Drawing.RectangleF]$Rectangle,
        [float]$Radius
    )

    $diameter = $Radius * 2.0
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc($Rectangle.X, $Rectangle.Y, $diameter, $diameter, 180.0, 90.0)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Y, $diameter, $diameter, 270.0, 90.0)
    $path.AddArc($Rectangle.Right - $diameter, $Rectangle.Bottom - $diameter, $diameter, $diameter, 0.0, 90.0)
    $path.AddArc($Rectangle.X, $Rectangle.Bottom - $diameter, $diameter, $diameter, 90.0, 90.0)
    $path.CloseFigure()
    return $path
}

function New-ScaledBitmap
{
    param(
        [System.Drawing.Bitmap]$Source,
        [int]$Size
    )

    $result = New-Object System.Drawing.Bitmap($Size, $Size,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($result)
    try
    {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.DrawImage($Source, (New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)))
    }
    finally
    {
        $graphics.Dispose()
    }
    return $result
}

function Get-IconDibBytes
{
    param([System.Drawing.Bitmap]$Bitmap)

    $width = $Bitmap.Width
    $height = $Bitmap.Height
    $pixelBytes = $width * $height * 4
    $maskRowBytes = [int](([Math]::Floor(($width + 31) / 32)) * 4)
    $maskBytes = $maskRowBytes * $height
    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)
    try
    {
        $writer.Write([int]40)
        $writer.Write([int]$width)
        $writer.Write([int]($height * 2))
        $writer.Write([int16]1)
        $writer.Write([int16]32)
        $writer.Write([int]0)
        $writer.Write([int]$pixelBytes)
        $writer.Write([int]0)
        $writer.Write([int]0)
        $writer.Write([int]0)
        $writer.Write([int]0)

        for ($y = $height - 1; $y -ge 0; $y--)
        {
            for ($x = 0; $x -lt $width; $x++)
            {
                $colour = $Bitmap.GetPixel($x, $y)
                $writer.Write([byte]$colour.B)
                $writer.Write([byte]$colour.G)
                $writer.Write([byte]$colour.R)
                $writer.Write([byte]$colour.A)
            }
        }

        for ($y = $height - 1; $y -ge 0; $y--)
        {
            [byte[]]$maskRow = New-Object byte[] $maskRowBytes
            for ($x = 0; $x -lt $width; $x++)
            {
                if ($Bitmap.GetPixel($x, $y).A -lt 128)
                {
                    $byteIndex = [int][Math]::Floor($x / 8)
                    $bit = 0x80 -shr ($x % 8)
                    $maskRow[$byteIndex] = [byte]($maskRow[$byteIndex] -bor $bit)
                }
            }
            $writer.Write($maskRow)
        }

        $writer.Flush()
        return $stream.ToArray()
    }
    finally
    {
        $writer.Dispose()
        $stream.Dispose()
    }
}

$source = [System.Drawing.Bitmap]::FromFile($SourcePath)
$scaledSource = $null
$finalPng = $null
try
{
    $scaledSource = New-ScaledBitmap $source 1024
    $finalPng = New-Object System.Drawing.Bitmap(1024, 1024,
        [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($finalPng)
    $path = New-RoundedRectanglePath (New-Object System.Drawing.RectangleF(4.0, 4.0, 1016.0, 1016.0)) 244.0
    $brush = New-Object System.Drawing.TextureBrush($scaledSource)
    try
    {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.FillPath($brush, $path)
    }
    finally
    {
        $brush.Dispose()
        $path.Dispose()
        $graphics.Dispose()
    }

    $finalPng.Save($PngPath, [System.Drawing.Imaging.ImageFormat]::Png)

    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $frames = @()
    foreach ($size in $sizes)
    {
        $frameBitmap = New-ScaledBitmap $finalPng $size
        try
        {
            $frames += New-Object PSObject -Property @{
                Size = $size
                Data = (Get-IconDibBytes $frameBitmap)
            }
        }
        finally
        {
            $frameBitmap.Dispose()
        }
    }

    $iconStream = New-Object System.IO.MemoryStream
    $iconWriter = New-Object System.IO.BinaryWriter($iconStream)
    try
    {
        $iconWriter.Write([int16]0)
        $iconWriter.Write([int16]1)
        $iconWriter.Write([int16]$frames.Count)
        $offset = 6 + 16 * $frames.Count
        foreach ($frame in $frames)
        {
            $dimension = if ($frame.Size -eq 256) { 0 } else { $frame.Size }
            $iconWriter.Write([byte]$dimension)
            $iconWriter.Write([byte]$dimension)
            $iconWriter.Write([byte]0)
            $iconWriter.Write([byte]0)
            $iconWriter.Write([int16]1)
            $iconWriter.Write([int16]32)
            $iconWriter.Write([int]$frame.Data.Length)
            $iconWriter.Write([int]$offset)
            $offset += $frame.Data.Length
        }
        foreach ($frame in $frames)
        {
            $iconWriter.Write([byte[]]$frame.Data)
        }
        $iconWriter.Flush()
        [System.IO.File]::WriteAllBytes($IconPath, $iconStream.ToArray())
    }
    finally
    {
        $iconWriter.Dispose()
        $iconStream.Dispose()
    }
}
finally
{
    if ($finalPng -ne $null) { $finalPng.Dispose() }
    if ($scaledSource -ne $null) { $scaledSource.Dispose() }
    $source.Dispose()
}

Write-Output ("Created {0} and {1}" -f $PngPath, $IconPath)
