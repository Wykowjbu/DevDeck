# Tài liệu thiết kế: Cấu hình Logo cho ứng dụng DevDeck

Tài liệu này mô tả chi tiết phương án thay thế và cấu hình logo mới cho ứng dụng DevDeck từ file ảnh nguồn tại `D:\Users\hantu\Downloads\logo.png`.

---

## 1. Phân tích ảnh nguồn và giới hạn vật lý
- **Đường dẫn ảnh nguồn**: `D:\Users\hantu\Downloads\logo.png`
- **Kích thước ảnh nguồn**: `96 × 96` pixel.
- **Lưu ý chất lượng**: Do ảnh nguồn có kích thước nhỏ (`96x96`), các asset có kích thước lớn hơn 96px (ví dụ: `Square150x150Logo.scale-200` kích thước 300x300px) khi tạo ra sẽ là bản **upscale** và **có thể bị mờ** trên các màn hình có mật độ điểm ảnh cao (High-DPI). Các asset nhỏ hơn hoặc bằng 96px sẽ được downscale nên vẫn giữ được độ sắc nét.

---

## 2. Xác định các Asset cần tạo (Dựa trên `Package.appxmanifest` và `.csproj`)
Dự án không sử dụng tính năng Lock Screen (`<uap:LockScreen>` không được khai báo), do đó **không thay đổi hoặc tạo mới LockScreenLogo**.

Các asset thực tế cần thay đổi bao gồm:

| Tên File Asset | Kích thước Yêu cầu (px) | Phương pháp Xử lý | Ghi chú |
| :--- | :--- | :--- | :--- |
| `Square44x44Logo.targetsize-24_altform-unplated.png` | 24 × 24 | Downscale từ 96x96 | Dùng cho MainWindow Title bar & Taskbar |
| `StoreLogo.png` | 50 × 50 | Downscale từ 96x96 | Logo hiển thị trên Microsoft Store |
| `Square44x44Logo.scale-200.png` | 88 × 88 | Downscale từ 96x96 | Dùng cho Start Menu (cỡ nhỏ) |
| `Square150x150Logo.scale-200.png` | 300 × 300 | Upscale từ 96x96 | Dùng cho Start Menu (cỡ trung bình). *Có thể bị mờ nhẹ.* |
| `Wide310x150Logo.scale-200.png` | 620 × 300 | Tạo canvas `620x300` trong suốt, đặt logo `96x96` ở chính giữa | Tránh kéo giãn méo ảnh |
| `SplashScreen.scale-200.png` | 1240 × 600 | Tạo canvas `1240x600` trong suốt, đặt logo `96x96` ở chính giữa | Tránh kéo giãn màn hình chào |

---

## 3. Cấu hình Hỗ trợ Chạy Unpackaged (File `.ico` cho File Explorer & Taskbar)
Do ứng dụng cấu hình `<WindowsPackageType>None</WindowsPackageType>` (chạy không đóng gói - Unpackaged), file `.exe` khi biên dịch ra cần một file `.ico` nhúng trực tiếp để hiển thị icon trên Windows File Explorer:
1. **Tạo file `Assets\app.ico`**: Ghép các frame ảnh PNG chất lượng cao có kích thước `16x16`, `24x24`, `32x32`, `48x48`, `96x96` được tạo từ ảnh gốc.
2. **Cập nhật `DevDeck.csproj`**: Thêm cấu hình `<ApplicationIcon>Assets\app.ico</ApplicationIcon>` vào nhóm `<PropertyGroup>` chính của dự án.

---

## 4. Kế hoạch Triển khai An toàn
Để tránh rủi ro mất mát hoặc hỏng hóc tài nguyên cũ, quy trình thực hiện sẽ tuân thủ nghiêm ngặt các bước sau:
1. **Sao lưu**: Tạo bản sao lưu thư mục `Assets` hiện tại thành `Assets_Backup`.
2. **Tạo Asset tạm**: Sinh ra toàn bộ các asset mới vào thư mục tạm `Assets_New` bằng tập lệnh PowerShell tự động sử dụng thư viện .NET WPF (`System.Windows.Media.Imaging`).
3. **Kiểm tra sơ bộ**: Xác minh kích thước, độ trong suốt và tỷ lệ của các file trong `Assets_New`.
4. **Áp dụng**: Copy các asset mới đè lên thư mục `Assets`.
5. **Cập nhật cấu hình**: Thêm thẻ `<ApplicationIcon>` vào `.csproj`.
6. **Xác minh thực tế**:
   - Chạy lệnh `dotnet build -c Release` để biên dịch ứng dụng.
   - Chạy thử bản Release để kiểm tra icon hiển thị thực tế trên Title bar, Taskbar và File Explorer.
