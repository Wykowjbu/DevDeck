# Tài liệu thiết kế: Cấu hình Logo cho ứng dụng DevDeck (MSIX Packaged Only)

Tài liệu này mô tả chi tiết phương án thay thế và cấu hình logo mới cho ứng dụng DevDeck phát hành dưới dạng MSIX từ file ảnh nguồn tại `D:\Users\hantu\Downloads\logo.png`.

---

## 1. Phân tích ảnh nguồn và giới hạn vật lý
- **Đường dẫn ảnh nguồn**: `D:\Users\hantu\Downloads\logo.png`
- **Kích thước ảnh nguồn**: `96 × 96` pixel.
- **Lưu ý chất lượng**: 
  - Các asset có kích thước nhỏ hơn hoặc bằng 96px (ví dụ: 24x24, 44x44, 88x88, 50x50) sẽ được downscale và giữ chất lượng tốt.
  - Các asset lớn hơn 96px (ví dụ: `Square150x150Logo.scale-200` kích thước 300x300px) sẽ là bản **upscale** và **có thể bị mờ** trên các màn hình có mật độ điểm ảnh cao (High-DPI).
  - Dự án này không cam kết logo sẽ sắc nét hoàn toàn trên mọi mật độ màn hình do giới hạn kích thước của ảnh nguồn (96x96px).

---

## 2. Xác định các Asset cần tạo (Dựa trên `Package.appxmanifest` và `.csproj`)
Dự án chỉ phát hành dưới dạng MSIX. Không thay đổi hoặc tạo mới LockScreenLogo vì manifest không cấu hình tính năng này.

Các asset thực tế cần tạo bao gồm:

### A. Nhóm Square44x44Logo (Cho Start Menu cỡ nhỏ, Taskbar, Search, Alt+Tab)
Tạo đầy đủ các scale và targetsize cần thiết để hiển thị chính xác trên Windows Shell:
- `Square44x44Logo.scale-200.png` (88 × 88 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-24_altform-unplated.png` (24 × 24 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-16.png` (16 × 16 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-24.png` (24 × 24 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-32.png` (32 × 32 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-48.png` (48 × 48 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-256.png` (256 × 256 px) - Upscale từ 96x96.
- `Square44x44Logo.targetsize-16_altform-unplated.png` (16 × 16 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-32_altform-unplated.png` (32 × 32 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-48_altform-unplated.png` (48 × 48 px) - Downscale từ 96x96.
- `Square44x44Logo.targetsize-256_altform-unplated.png` (256 × 256 px) - Upscale từ 96x96.

### B. Nhóm Logo Khác (Dựa theo `Package.appxmanifest`)
- `StoreLogo.png` (50 × 50 px) - Downscale từ 96x96.
- `Square150x150Logo.scale-200.png` (300 × 300 px) - Upscale từ 96x96 (bản này có thể mờ nhẹ).
- `Wide310x150Logo.scale-200.png` (620 × 300 px) - Tạo canvas `620x300` trong suốt, đặt logo ở chính giữa. Logo sẽ được hiển thị ở kích thước hợp lý (ví dụ: upscale nhẹ lên 150x150px hoặc giữ nguyên 96x96px tùy theo độ cân đối trực quan, chấp nhận upscale có thể mờ để đảm bảo tỷ lệ hợp lý với canvas).
- `SplashScreen.scale-200.png` (1240 × 600 px) - Tạo canvas `1240x600` trong suốt, đặt logo ở chính giữa. Logo hiển thị ở kích thước hợp lý (ví dụ: upscale lên 200x200px hoặc giữ nguyên 96x96px, chấp nhận mờ để không quá nhỏ so với màn hình chào 1240x600).

---

## 3. Lựa chọn Công cụ Tạo Asset
1. **Ưu tiên 1**: Sử dụng công cụ chính thức của Microsoft `winapp manifest update-assets` nếu tương thích với môi trường.
2. **Ưu tiên 2 (Mặc định)**: Do thử nghiệm sơ bộ `winapp` CLI chưa sẵn sàng, dự án sẽ sử dụng một script PowerShell xử lý ảnh riêng bằng thư viện .NET WPF (`System.Windows.Media.Imaging`). Script này cam kết:
   - Không làm méo logo (luôn giữ tỷ lệ 1:1 của logo gốc).
   - Giữ nguyên độ trong suốt (transparency).
   - Sử dụng bộ lọc resize chất lượng cao (`Fant` hoặc `HighQualityBicubic`).
   - Tự động kiểm tra và xác minh kích thước đầu ra trước khi ghi đè.

---

## 4. Quy trình Triển khai Chi tiết (9 bước)
1. **Sao lưu**: Sử dụng Git để commit hoặc tạo nhánh sao lưu trạng thái hiện tại của dự án.
2. **Kiểm tra manifest**: Xác định chính xác các file đang được tham chiếu trong [Package.appxmanifest](file:///D:/Users/huynp29052004/Projects/DevDeck/DevDeck/Package.appxmanifest).
3. **Tạo asset tạm**: Sinh ra toàn bộ các asset mới vào thư mục tạm nằm ngoài dự án (`D:\Users\huynp29052004\Projects\DevDeck_Assets_Temp`).
4. **Kiểm tra & Xác minh**: Xác minh kích thước, độ trong suốt, và tỷ lệ hiển thị của các asset trong thư mục tạm.
5. **Ghi đè vào dự án**: Copy các asset đã kiểm tra đè vào thư mục `Assets` của dự án và cập nhật `DevDeck.csproj` để khai báo các file mới nếu cần.
6. **Build MSIX**: Biên dịch dự án và đóng gói MSIX ở cấu hình Release.
7. **Gỡ bỏ bản cũ**: Gỡ cài đặt phiên bản DevDeck cũ trên máy Windows để đảm bảo hệ thống xóa sạch bộ nhớ cache icon.
8. **Cài đặt gói mới**: Tiến hành cài đặt gói MSIX vừa build sạch sẽ.
9. **Kiểm tra thực tế**: Xác minh logo hiển thị thực tế tại các vị trí sau trên Windows:
   - Start Menu (cỡ nhỏ, cỡ trung bình, ô Tile)
   - Windows Search
   - Taskbar (khi ghim ứng dụng và khi đang chạy)
   - Tổ hợp phím Alt + Tab
   - Splash Screen (Màn hình chào khi mở app)
   - Settings > Apps & features (Ứng dụng và tính năng)
   - Thư mục Package được cài đặt trong Windows
