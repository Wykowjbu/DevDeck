$msixPath = "D:\Users\huynp29052004\Projects\DevDeck\DevDeck\AppPackages\DevDeck_1.0.0.0_x64_Test\DevDeck_1.0.0.0_x64.msix"
$signtool = "C:\Users\hantu\.nuget\packages\microsoft.windows.sdk.buildtools\10.0.28000.1839\bin\10.0.28000.0\x64\signtool.exe"

Write-Host "Creating self-signed certificate for CN=hantu..." -ForegroundColor Cyan
# Tạo cert tự ký khớp với Publisher của ứng dụng
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=hantu" -KeyUsage DigitalSignature -FriendlyName "DevDeck Test Cert" -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

# Đường dẫn lưu cert tạm thời
$tempCer = "D:\Users\huynp29052004\Projects\DevDeck\DevDeck\scratch\temp_cert.cer"
Export-Certificate -Cert $cert -FilePath $tempCer

Write-Host "Importing certificate to Trusted People (Current User)..." -ForegroundColor Cyan
# Import cert để Windows chấp nhận cài đặt (Current User, không cần quyền Admin)
Import-Certificate -FilePath $tempCer -CertStoreLocation Cert:\CurrentUser\TrustedPeople

Write-Host "Signing MSIX package..." -ForegroundColor Cyan
# Thực hiện ký số bằng signtool từ NuGet package
& $signtool sign /sha1 $cert.Thumbprint /fd SHA256 /a $msixPath

# Xóa file cert tạm thời
Remove-Item $tempCer -Force

Write-Host "Installing signed MSIX package..." -ForegroundColor Cyan
# Thực hiện cài đặt gói sạch
Add-AppxPackage -Path $msixPath

Write-Host "Done! DevDeck installed successfully." -ForegroundColor Green
