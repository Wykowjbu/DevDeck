$dir = "D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp"
Add-Type -AssemblyName PresentationCore

$files = @{
    "Square44x44Logo.scale-200.png" = @(88, 88)
    "Square44x44Logo.targetsize-24_altform-unplated.png" = @(24, 24)
    "Square44x44Logo.targetsize-16.png" = @(16, 16)
    "Square44x44Logo.targetsize-24.png" = @(24, 24)
    "Square44x44Logo.targetsize-32.png" = @(32, 32)
    "Square44x44Logo.targetsize-48.png" = @(48, 48)
    "Square44x44Logo.targetsize-256.png" = @(256, 256)
    "Square44x44Logo.targetsize-16_altform-unplated.png" = @(16, 16)
    "Square44x44Logo.targetsize-32_altform-unplated.png" = @(32, 32)
    "Square44x44Logo.targetsize-48_altform-unplated.png" = @(48, 48)
    "Square44x44Logo.targetsize-256_altform-unplated.png" = @(256, 256)
    "StoreLogo.png" = @(50, 50)
    "Square150x150Logo.scale-200.png" = @(300, 300)
    "Wide310x150Logo.scale-200.png" = @(620, 300)
    "SplashScreen.scale-200.png" = @(1240, 600)
}

$hasError = $false

foreach ($file in $files.Keys) {
    $path = Join-Path $dir $file
    if (-not (Test-Path $path)) {
        Write-Error "Missing asset: $file"
        $hasError = $true
        continue
    }
    
    $stream = [System.IO.File]::OpenRead($path)
    $decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new($stream, [System.Windows.Media.Imaging.BitmapCreateOptions]::None, [System.Windows.Media.Imaging.BitmapCacheOption]::Default)
    $frame = $decoder.Frames[0]
    $stream.Close()
    
    $expected = $files[$file]
    if ($frame.PixelWidth -ne $expected[0] -or $frame.PixelHeight -ne $expected[1]) {
        Write-Error "Size mismatch on $file. Expected: $($expected[0])x$($expected[1]), Got: $($frame.PixelWidth)x$($frame.PixelHeight)"
        $hasError = $true
    } else {
        Write-Host "Verified $($file): $($frame.PixelWidth)x$($frame.PixelHeight) px - OK"
    }
}

if ($hasError) {
    Write-Host "Verification FAILED with errors!" -ForegroundColor Red
    exit 1
} else {
    Write-Host "All assets successfully verified!" -ForegroundColor Green
}
