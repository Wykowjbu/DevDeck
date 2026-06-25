using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DevDeck.Contracts;
using DevDeck.Data;
using DevDeck.Enums;
using DevDeck.Models;
using DevDeck.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace DevDeck.Features.Settings
{
    public sealed partial class SettingsPage : Page
    {
        private readonly ISettingsService _settingsService;
        private readonly IDataService _dataService;
        private readonly IDialogService _dialogService;
        private readonly WindowHandleService _handleService;
        private bool _isInitializing = true;

        public SettingsPage()
        {
            InitializeComponent();
            _settingsService = App.AppHost.Services.GetRequiredService<ISettingsService>();
            _dataService = App.AppHost.Services.GetRequiredService<IDataService>();
            _dialogService = App.AppHost.Services.GetRequiredService<IDialogService>();
            _handleService = App.AppHost.Services.GetRequiredService<WindowHandleService>();

            LoadSettings();
        }

        private void LoadSettings()
        {
            _isInitializing = true;
            var settings = _settingsService.Settings;

            // Load theme
            ThemeComboBox.SelectedIndex = settings.Theme switch
            {
                AppTheme.Light => 0,
                AppTheme.Dark => 1,
                AppTheme.System => 2,
                _ => 2
            };

            BackdropComboBox.SelectedIndex = settings.Backdrop switch
            {
                BackdropKind.Mica => 0,
                BackdropKind.MicaAlt => 1,
                BackdropKind.Solid => 2,
                _ => 0
            };

            // Load default shell
            DefaultShellComboBox.SelectedIndex = settings.DefaultShell switch
            {
                ShellType.PowerShell7 => 0,
                ShellType.WindowsPowerShell => 1,
                ShellType.CommandPrompt => 2,
                ShellType.GitBash => 3,
                _ => 2
            };

            ActionButtonSizeComboBox.SelectedIndex = settings.ActionButtonSize switch
            {
                ActionButtonSize.Compact => 0,
                ActionButtonSize.Standard => 1,
                ActionButtonSize.Large => 2,
                _ => 1
            };

            MaxConcurrentActionsInput.Value = Math.Clamp(settings.MaximumConcurrentActions, 1, 16);

            _isInitializing = false;
        }

        private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            var newTheme = ThemeComboBox.SelectedIndex switch
            {
                0 => AppTheme.Light,
                1 => AppTheme.Dark,
                2 => AppTheme.System,
                _ => AppTheme.System
            };

            _settingsService.Settings.Theme = newTheme;
            await _settingsService.SaveAsync();

            // Apply theme dynamically
            App.ApplyTheme(newTheme);
        }

        private async void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            var newBackdrop = BackdropComboBox.SelectedIndex switch
            {
                1 => BackdropKind.MicaAlt,
                2 => BackdropKind.Solid,
                _ => BackdropKind.Mica
            };

            _settingsService.Settings.Backdrop = newBackdrop;
            await _settingsService.SaveAsync();

            App.ApplyBackdrop(newBackdrop);
        }

        private async void DefaultShellComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            var newShell = DefaultShellComboBox.SelectedIndex switch
            {
                0 => ShellType.PowerShell7,
                1 => ShellType.WindowsPowerShell,
                2 => ShellType.CommandPrompt,
                3 => ShellType.GitBash,
                _ => ShellType.CommandPrompt
            };

            _settingsService.Settings.DefaultShell = newShell;
            await _settingsService.SaveAsync();
        }

        private async void ActionButtonSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            var newSize = ActionButtonSizeComboBox.SelectedIndex switch
            {
                0 => ActionButtonSize.Compact,
                2 => ActionButtonSize.Large,
                _ => ActionButtonSize.Standard
            };

            _settingsService.Settings.ActionButtonSize = newSize;
            await _settingsService.SaveAsync();
        }

        private async void MaxConcurrentActionsInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isInitializing) return;

            _settingsService.Settings.MaximumConcurrentActions = (int)Math.Clamp(sender.Value, 1, 16);
            await _settingsService.SaveAsync();
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var pathResolver = App.AppHost.Services.GetRequiredService<IPathResolverService>();
                string path = pathResolver.GetAppDataDirectory();
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                _ = _dialogService.ShowMessageAsync("Lỗi", $"Không thể mở thư mục dữ liệu: {ex.Message}", XamlRoot);
            }
        }

        private async void ExportConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("DevDeck Backup JSON", new[] { ".json" });
                savePicker.SuggestedFileName = $"devdeck_backup_{DateTime.Now:yyyyMMdd}";

                var hwnd = _handleService.WindowHandle;
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                var file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    // Create export package
                    var package = new BackupPackage
                    {
                        Data = _dataService.Data,
                        Settings = _settingsService.Settings
                    };

                    string json = JsonSerializer.Serialize(package, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(file.Path, json);
                    await _dialogService.ShowMessageAsync("Thành công", $"Xuất cấu hình backup ra file thành công!", XamlRoot);
                }
            }
            catch (Exception ex)
            {
                _ = _dialogService.ShowMessageAsync("Lỗi", $"Không thể xuất cấu hình: {ex.Message}", XamlRoot);
            }
        }

        private async void ImportConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openPicker = new FileOpenPicker();
                openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                openPicker.FileTypeFilter.Add(".json");

                var hwnd = _handleService.WindowHandle;
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string json = await File.ReadAllTextAsync(file.Path);
                    var package = JsonSerializer.Deserialize<BackupPackage>(json);
                    
                    if (package != null && package.Data != null && package.Settings != null)
                    {
                        bool confirm = await _dialogService.ShowConfirmationAsync(
                            "Xác nhận Import",
                            "Import file backup sẽ ghi đè toàn bộ dữ liệu hiện tại của DevDeck. Bạn có chắc chắn muốn tiếp tục?",
                            XamlRoot);

                        if (confirm)
                        {
                            // Override files
                            var pathResolver = App.AppHost.Services.GetRequiredService<IPathResolverService>();
                            
                            // Save data.json
                            string dataPath = Path.Combine(pathResolver.GetAppDataDirectory(), "data.json");
                            string dataJson = JsonSerializer.Serialize(package.Data, new JsonSerializerOptions { WriteIndented = true });
                            await File.WriteAllTextAsync(dataPath, dataJson);

                            // Save settings.json
                            string settingsPath = pathResolver.GetSettingsPath();
                            string settingsJson = JsonSerializer.Serialize(package.Settings, new JsonSerializerOptions { WriteIndented = true });
                            await File.WriteAllTextAsync(settingsPath, settingsJson);

                            await _dialogService.ShowMessageAsync("Hoàn tất", "Đã phục hồi cấu hình thành công! Hãy khởi động lại DevDeck để áp dụng các thay đổi mới.", XamlRoot);
                        }
                    }
                    else
                    {
                        await _dialogService.ShowMessageAsync("Lỗi", "File backup không đúng định dạng của DevDeck.", XamlRoot);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = _dialogService.ShowMessageAsync("Lỗi", $"Không thể nhập cấu hình: {ex.Message}", XamlRoot);
            }
        }

        private async void ResetApp_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Xác nhận Reset ứng dụng",
                "Hành động này sẽ XÓA TOÀN BỘ Workspaces, Projects và Actions của bạn trên DevDeck. \n\nThư mục Project trên đĩa cứng vẫn an toàn. \n\nBạn có muốn tiếp tục?",
                XamlRoot);

            if (confirm)
            {
                try
                {
                    var pathResolver = App.AppHost.Services.GetRequiredService<IPathResolverService>();
                    
                    string dataPath = Path.Combine(pathResolver.GetAppDataDirectory(), "data.json");
                    if (File.Exists(dataPath)) File.Delete(dataPath);

                    string settingsPath = pathResolver.GetSettingsPath();
                    if (File.Exists(settingsPath)) File.Delete(settingsPath);

                    await _dialogService.ShowMessageAsync("Hoàn tất", "DevDeck đã được reset về trạng thái ban đầu. Hãy khởi động lại ứng dụng.", XamlRoot);
                }
                catch (Exception ex)
                {
                    _ = _dialogService.ShowMessageAsync("Lỗi", $"Reset thất bại: {ex.Message}", XamlRoot);
                }
            }
        }

        private sealed class BackupPackage
        {
            public DevDeckData? Data { get; set; }
            public AppSettings? Settings { get; set; }
        }
    }
}
