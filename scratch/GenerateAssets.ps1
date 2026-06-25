Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$srcPath = "D:\Users\hantu\Downloads\logo.png"
$destDir = "D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp"

if (-not (Test-Path $srcPath)) {
    Write-Error "Source logo file not found at $srcPath"
    exit 1
}

# Load ảnh nguồn
$uri = [System.Uri]::new($srcPath)
$decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new($uri, [System.Windows.Media.Imaging.BitmapCreateOptions]::None, [System.Windows.Media.Imaging.BitmapCacheOption]::Default)
$srcFrame = $decoder.Frames[0]

# Hàm downscale/upscale trực tiếp ảnh vuông 1:1
function CreateDirectAsset($size, $filename) {
    $outputPath = Join-Path $destDir $filename
    
    # Tạo phép biển đổi kích thước với bộ lọc chất lượng cao (TransformedBitmap sử dụng Fant scale)
    $scaledLogo = [System.Windows.Media.Imaging.TransformedBitmap]::new($srcFrame, [System.Windows.Media.ScaleTransform]::new($size / $srcFrame.PixelWidth, $size / $srcFrame.PixelHeight))
    
    # Ghi ra file PNG
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($scaledLogo))
    $fs = [System.IO.File]::Create($outputPath)
    $encoder.Save($fs)
    $fs.Close()
    Write-Host "Created direct asset: $filename ($($size)x$($size) px)"
}

# Hàm vẽ logo căn giữa trên canvas trong suốt (không bị kéo giãn, giữ tỷ lệ 1:1)
function CreateCenteredAsset($destWidth, $destHeight, $logoSize, $filename) {
    $outputPath = Join-Path $destDir $filename
    
    # Resize logo sang kích thước $logoSize
    $scaledLogo = [System.Windows.Media.Imaging.TransformedBitmap]::new($srcFrame, [System.Windows.Media.ScaleTransform]::new($logoSize / $srcFrame.PixelWidth, $logoSize / $srcFrame.PixelHeight))
    
    # Tạo drawing context để vẽ lên canvas trong suốt
    $visual = [System.Windows.Media.DrawingVisual]::new()
    $context = $visual.RenderOpen()
    
    # Tính toán vị trí căn giữa
    $x = ($destWidth - $logoSize) / 2
    $y = ($destHeight - $logoSize) / 2
    
    $rect = [System.Windows.Rect]::new($x, $y, $logoSize, $logoSize)
    $context.DrawImage($scaledLogo, $rect)
    $context.Close()
    
    # Render ra bitmap 32-bit (hỗ trợ Alpha/Transparency)
    $rtb = [System.Windows.Media.Imaging.RenderTargetBitmap]::new($destWidth, $destHeight, 96, 96, [System.Windows.Media.PixelFormats]::Pbgra32)
    $rtb.Render($visual)
    
    # Ghi ra file PNG
    $encoder = [System.Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($rtb))
    $fs = [System.IO.File]::Create($outputPath)
    $encoder.Save($fs)
    $fs.Close()
    Write-Host "Created centered asset: $filename ($($destWidth)x$($destHeight) px, logo size $($logoSize)x$($logoSize) px)"
}

# --- THỰC THI TẠO CÁC ASSET ---

# 1. Nhóm Square44x44Logo
CreateDirectAsset 88 "Square44x44Logo.scale-200.png"
CreateDirectAsset 24 "Square44x44Logo.targetsize-24_altform-unplated.png"
CreateDirectAsset 16 "Square44x44Logo.targetsize-16.png"
CreateDirectAsset 24 "Square44x44Logo.targetsize-24.png"
CreateDirectAsset 32 "Square44x44Logo.targetsize-32.png"
CreateDirectAsset 48 "Square44x44Logo.targetsize-48.png"
CreateDirectAsset 256 "Square44x44Logo.targetsize-256.png"
CreateDirectAsset 16 "Square44x44Logo.targetsize-16_altform-unplated.png"
CreateDirectAsset 32 "Square44x44Logo.targetsize-32_altform-unplated.png"
CreateDirectAsset 48 "Square44x44Logo.targetsize-48_altform-unplated.png"
CreateDirectAsset 256 "Square44x44Logo.targetsize-256_altform-unplated.png"

# 2. Nhóm các logo chính khác
CreateDirectAsset 50 "StoreLogo.png"
CreateDirectAsset 300 "Square150x150Logo.scale-200.png"

# 3. Nhóm logo canvas rộng (căn giữa)
CreateCenteredAsset 620 300 96 "Wide310x150Logo.scale-200.png"
CreateCenteredAsset 1240 600 192 "SplashScreen.scale-200.png"

Write-Host "Asset generation completed!"
