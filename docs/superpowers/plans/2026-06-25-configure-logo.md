# Cấu hình Logo cho ứng dụng DevDeck (MSIX Packaged) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thay thế toàn bộ hệ thống logo của ứng dụng DevDeck bằng logo mới từ ảnh nguồn `logo.png` (96x96px), tạo đầy đủ các scale/targetsize cho Square44x44Logo, xử lý canvas căn giữa cho các ảnh không vuông để tránh méo và cấu hình MSIX Packaged.

**Architecture:** Sử dụng script PowerShell (.NET WPF) để xử lý ảnh chất lượng cao trực tiếp trên Windows, tạo ra các asset mới trong một thư mục tạm ngoài dự án, kiểm tra kích thước và độ trong suốt trước khi ghi đè vào thư mục `Assets` của dự án. Sau đó cập nhật cấu hình `.csproj` để MSBuild đóng gói các asset mới và build MSIX Release để kiểm tra thực tế.

**Tech Stack:** C#, .NET 8, WinUI 3, PowerShell (.NET WPF imaging libraries), MSBuild / dotnet CLI.

## Global Constraints

- Chỉ tập trung vào các asset được `Package.appxmanifest` sử dụng.
- Không thay đổi hoặc tạo `LockScreenLogo`.
- Không tạo hoặc cấu hình file `app.ico` cho file `.exe`.
- Không thêm `<ApplicationIcon>` vào `.csproj`.
- Không gọi `AppWindow.SetIcon()` trong code C#.
- Do ảnh nguồn có kích thước nhỏ (`96x96`), các asset lớn hơn 96px sẽ là bản upscale và có thể bị mờ.
- Luôn giữ đúng tỷ lệ logo 1:1, không kéo giãn.
- Với `Wide310x150Logo` và `SplashScreen`, phải tạo canvas đúng kích thước, sau đó căn logo vào giữa trên nền trong suốt.

---

### Task 1: Sao lưu trạng thái dự án và chuẩn bị thư mục tạm

**Files:**
- Modify: `D:\Users\huynp29052004\Projects\DevDeck\DevDeck` (Sao lưu toàn bộ thư mục `Assets`)

**Interfaces:**
- Consumes: Thư mục `Assets` hiện tại.
- Produces: Bản sao lưu `Assets_Backup` trong thư mục dự án và thư mục tạm `D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp` bên ngoài dự án.

- [ ] **Step 1: Commit trạng thái hiện tại của Git**
  Chạy lệnh để đảm bảo thư mục làm việc sạch sẽ:
  ```powershell
  git add .
  git commit -m "pre-logo: backup before asset generation"
  ```
  Expected: Git commit thành công.

- [ ] **Step 2: Tạo thư mục tạm bên ngoài dự án**
  Tạo thư mục tạm `D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp` để chứa các asset mới chuẩn bị tạo:
  ```powershell
  New-Item -ItemType Directory -Path "D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp" -Force
  ```
  Expected: Thư mục được tạo thành công.

- [ ] **Step 3: Tạo bản sao lưu thư mục Assets cũ**
  Sao lưu thư mục `Assets` hiện tại sang `Assets_Backup` để phục hồi nếu cần:
  ```powershell
  Copy-Item -Path "D:\Users\huynp29052004\Projects\DevDeck\DevDeck\Assets" -Destination "D:\Users\huynp29052004\Projects\DevDeck\DevDeck\Assets_Backup" -Recurse -Force
  ```
  Expected: Thư mục `Assets_Backup` được tạo đầy đủ các file ảnh cũ.

---

### Task 2: Viết script PowerShell xử lý hình ảnh

**Files:**
- Create: `D:\Users\huynp29052004\Projects\DevDeck\DevDeck\scratch\GenerateAssets.ps1`

**Interfaces:**
- Consumes: Ảnh nguồn tại `D:\Users\hantu\Downloads\logo.png`.
- Produces: Tập lệnh PowerShell sử dụng thư viện .NET WPF để resize ảnh chất lượng cao và tạo canvas trong suốt.

- [ ] **Step 1: Tạo file script GenerateAssets.ps1**
  Tạo file script PowerShell tại đường dẫn `D:\Users\huynp29052004\Projects\DevDeck\DevDeck\scratch\GenerateAssets.ps1` với nội dung xử lý ảnh:
  ```powershell
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
  # Wide310x150Logo.scale-200 (620x300 px), đặt logo 96x96 ở giữa (không bị mờ do giữ nguyên size gốc)
  CreateCenteredAsset 620 300 96 "Wide310x150Logo.scale-200.png"

  # SplashScreen.scale-200 (1240x600 px), đặt logo 192x192 ở giữa (chấp nhận upscale chẵn 2x để trông cân đối)
  CreateCenteredAsset 1240 600 192 "SplashScreen.scale-200.png"

  Write-Host "Asset generation completed!"
  ```
  Expected: File script được ghi thành công không có lỗi cú pháp.

- [ ] **Step 2: Commit file script**
  ```powershell
  git add scratch/GenerateAssets.ps1
  git commit -m "tool: add asset generator script"
  ```
  Expected: Commit thành công.

---

### Task 3: Chạy script sinh asset và xác minh kích thước

**Files:**
- Modify: `D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp` (Tạo các file ảnh mới)

**Interfaces:**
- Consumes: Script `scratch/GenerateAssets.ps1`.
- Produces: Đầy đủ các file ảnh logo mới trong thư mục tạm.

- [ ] **Step 1: Thực thi script PowerShell**
  Chạy lệnh để sinh ảnh:
  ```powershell
  powershell -ExecutionPolicy Bypass -File "D:\Users\huynp29052004\Projects\DevDeck\DevDeck\scratch\GenerateAssets.ps1"
  ```
  Expected: Console xuất ra các dòng "Created direct/centered asset..." và kết thúc bằng "Asset generation completed!".

- [ ] **Step 2: Viết script tự động xác minh kích thước file ảnh**
  Chạy script kiểm tra xem toàn bộ các file ảnh đã được sinh ra chính xác với kích thước yêu cầu hay chưa:
  ```powershell
  powershell -Command "
  \$dir = 'D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp';
  Add-Type -AssemblyName PresentationCore;
  \$files = @{
      'Square44x44Logo.scale-200.png' = @(88, 88);
      'Square44x44Logo.targetsize-24_altform-unplated.png' = @(24, 24);
      'Square44x44Logo.targetsize-16.png' = @(16, 16);
      'Square44x44Logo.targetsize-24.png' = @(24, 24);
      'Square44x44Logo.targetsize-32.png' = @(32, 32);
      'Square44x44Logo.targetsize-48.png' = @(48, 48);
      'Square44x44Logo.targetsize-256.png' = @(256, 256);
      'Square44x44Logo.targetsize-16_altform-unplated.png' = @(16, 16);
      'Square44x44Logo.targetsize-32_altform-unplated.png' = @(32, 32);
      'Square44x44Logo.targetsize-48_altform-unplated.png' = @(48, 48);
      'Square44x44Logo.targetsize-256_altform-unplated.png' = @(256, 256);
      'StoreLogo.png' = @(50, 50);
      'Square150x150Logo.scale-200.png' = @(300, 300);
      'Wide310x150Logo.scale-200.png' = @(620, 300);
      'SplashScreen.scale-200.png' = @(1240, 600)
  };
  foreach (\$file in \$files.Keys) {
      \$path = Join-Path \$dir \$file;
      if (-not (Test-Path \$path)) { Write-Error \"Missing: \$file\"; continue; }
      \$stream = [System.IO.File]::OpenRead(\$path);
      \$decoder = [System.Windows.Media.Imaging.PngBitmapDecoder]::new(\$stream, [System.Windows.Media.Imaging.BitmapCreateOptions]::None, [System.Windows.Media.Imaging.BitmapCacheOption]::Default);
      \$frame = \$decoder.Frames[0];
      \$stream.Close();
      \$expected = \$files[\$file];
      if (\$frame.PixelWidth -ne \$expected[0] -or \$frame.PixelHeight -ne \$expected[1]) {
          Write-Error \"Size mismatch on \$file. Expected: \$(\$expected[0])x\$(\$expected[1]), Got: \$(\$frame.PixelWidth)x\$(\$frame.PixelHeight)\";
      } else {
          Write-Host \"Verified \$file is \$(\$frame.PixelWidth)x\$(\$frame.PixelHeight) px\"
      }
  }
  "
  ```
  Expected: Toàn bộ các file được "Verified" thành công và không có lỗi "Size mismatch" hay "Missing".

---

### Task 4: Thay thế các file trong thư mục dự án và cập nhật cấu hình

**Files:**
- Modify: `D:\Users\huynp29052004\Projects\DevDeck\DevDeck\Assets` (Ghi đè và thêm các file logo mới)
- Modify: `D:\Users\huynp29052004\Projects\DevDeck\DevDeck\DevDeck.csproj:18-27` (Đăng ký các file targetsize mới trong csproj)

**Interfaces:**
- Consumes: Asset đã xác minh ở thư mục tạm.
- Produces: Thư mục `Assets` của dự án được cập nhật mới và cấu hình `.csproj` khai báo đầy đủ các file.

- [ ] **Step 1: Sao chép asset mới vào dự án**
  Chạy lệnh PowerShell để ghi đè các asset đã sinh vào thư mục `Assets` của ứng dụng:
  ```powershell
  Copy-Item -Path "D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp\*" -Destination "D:\Users\huynp29052004\Projects\DevDeck\DevDeck\Assets" -Force
  ```
  Expected: Các file được sao chép thành công vào thư mục `Assets`.

- [ ] **Step 2: Cập nhật DevDeck.csproj để khai báo các file targetsize mới**
  Chỉnh sửa file [DevDeck.csproj](file:///D:/Users/huynp29052004/Projects/DevDeck/DevDeck/DevDeck.csproj#L18-L27) để thêm các Content Include tương ứng.
  
  Thay thế đoạn:
  ```xml
    <ItemGroup>
      <Content Include="Assets\SplashScreen.scale-200.png" />
      <Content Include="Assets\LockScreenLogo.scale-200.png" />
      <Content Include="Assets\Square150x150Logo.scale-200.png" />
      <Content Include="Assets\Square44x44Logo.scale-200.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-24_altform-unplated.png" />
      <Content Include="Assets\StoreLogo.png" />
      <Content Include="Assets\Wide310x150Logo.scale-200.png" />
    </ItemGroup>
  ```
  Thành:
  ```xml
    <ItemGroup>
      <Content Include="Assets\SplashScreen.scale-200.png" />
      <Content Include="Assets\LockScreenLogo.scale-200.png" />
      <Content Include="Assets\Square150x150Logo.scale-200.png" />
      <Content Include="Assets\Square44x44Logo.scale-200.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-24_altform-unplated.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-16.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-24.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-32.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-48.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-256.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-16_altform-unplated.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-32_altform-unplated.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-48_altform-unplated.png" />
      <Content Include="Assets\Square44x44Logo.targetsize-256_altform-unplated.png" />
      <Content Include="Assets\StoreLogo.png" />
      <Content Include="Assets\Wide310x150Logo.scale-200.png" />
    </ItemGroup>
  ```
  Expected: File `.csproj` được lưu lại chính xác.

- [ ] **Step 3: Commit thay đổi trong dự án**
  ```powershell
  git add DevDeck.csproj Assets/
  git commit -m "feat: configure and copy all MSIX logo assets"
  ```
  Expected: Git commit thành công.

---

### Task 5: Build MSIX Release và kiểm tra thực tế

**Files:**
- Modify: `D:\Users\huynp29052004\Projects\DevDeck\DevDeck` (Biên dịch dự án và tạo package MSIX)

**Interfaces:**
- Consumes: Cấu trúc file và Assets hiện tại.
- Produces: File gói MSIX biên dịch thành công.

- [ ] **Step 1: Biên dịch và xuất bản dự án ở cấu hình Release**
  Chạy lệnh build MSIX ở cấu hình Release (hoặc Debug nếu cần kiểm tra nhanh):
  ```powershell
  dotnet publish -c Release /p:GenerateAppxPackageOnBuild=true
  ```
  Expected: Lệnh biên dịch thành công và tạo ra gói MSIX trong thư mục `bin\Release\net8.0-windows10.0.19041.0\win-x64\publish` hoặc đường dẫn xuất bản tương ứng.

- [ ] **Step 2: Gỡ cài đặt bản DevDeck cũ trên máy Windows**
  Để xóa cache icon cũ của Windows, gỡ ứng dụng cũ:
  ```powershell
  powershell -Command "Get-AppxPackage -Name *DevDeck* | Remove-AppxPackage"
  ```
  Expected: Gỡ cài đặt thành công bản DevDeck cũ.

- [ ] **Step 3: Cài đặt gói MSIX mới**
  Tìm file MSIX trong thư mục đầu ra và cài đặt (hoặc chạy trực tiếp gói để cài). Hướng dẫn người dùng nhấp đúp vào file MSIX hoặc chạy lệnh PowerShell để cài gói mới.

- [ ] **Step 4: Kiểm tra trực quan các vị trí**
  Mở ứng dụng và kiểm tra thực tế hiển thị logo tại:
  - Start Menu (cỡ nhỏ, cỡ trung)
  - Windows Search
  - Taskbar (khi ghim và khi ứng dụng đang chạy)
  - Alt+Tab
  - Splash Screen (Màn hình chào)
  - Settings > Apps & features
