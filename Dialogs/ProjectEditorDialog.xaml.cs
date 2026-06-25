using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Models;
using DevDeck.Enums;
using DevDeck.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DevDeck.Dialogs
{
    public sealed partial class ProjectEditorDialog : ContentDialog
    {
        public ProjectEntity? ProjectResult { get; private set; }
        private readonly Guid _workspaceId;
        private readonly ProjectEntity? _existing;

        public ProjectEditorDialog(Guid workspaceId, ProjectEntity? existing)
        {
            InitializeComponent();
            _workspaceId = workspaceId;
            _existing = existing;

            if (_existing != null)
            {
                Title = "Chỉnh sửa Project";
                ProjectNameInput.Text = _existing.Name;
                FolderPathInput.Text = _existing.FolderPath;
                IsPinnedInput.IsChecked = _existing.IsPinned;

                int shellIndex = _existing.DefaultShell switch
                {
                    ShellType.PowerShell7 => 0,
                    ShellType.WindowsPowerShell => 1,
                    ShellType.CommandPrompt => 2,
                    ShellType.GitBash => 3,
                    _ => 0
                };
                DefaultShellInput.SelectedIndex = shellIndex;
            }
            else
            {
                Title = "Thêm Project Mới";
            }

            PrimaryButtonClick += ProjectEditorDialog_PrimaryButtonClick;
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            // Resolve WindowHandleService from AppHost
            var handleService = App.AppHost.Services.GetRequiredService<WindowHandleService>();
            var hwnd = handleService.WindowHandle;
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                FolderPathInput.Text = folder.Path;
                
                // If name is empty, auto fill with folder name
                if (string.IsNullOrEmpty(ProjectNameInput.Text.Trim()))
                {
                    ProjectNameInput.Text = Path.GetFileName(folder.Path);
                }
            }
        }

        private void ProjectEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string name = ProjectNameInput.Text.Trim();
            string folderPath = FolderPathInput.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                args.Cancel = true;
                ProjectNameInput.Header = "Tên hiển thị (Không được để trống)";
                return;
            }

            if (string.IsNullOrEmpty(folderPath))
            {
                args.Cancel = true;
                FolderPathInput.Header = "Đường dẫn thư mục (Không được để trống)";
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                args.Cancel = true;
                FolderPathInput.Header = "Thư mục không tồn tại trên thực tế";
                return;
            }

            ShellType defaultShell = DefaultShellInput.SelectedIndex switch
            {
                0 => ShellType.PowerShell7,
                1 => ShellType.WindowsPowerShell,
                2 => ShellType.CommandPrompt,
                3 => ShellType.GitBash,
                _ => ShellType.PowerShell7
            };

            ProjectResult = new ProjectEntity
            {
                Id = _existing?.Id ?? Guid.Empty,
                WorkspaceId = _existing?.WorkspaceId ?? _workspaceId,
                Name = name,
                FolderPath = folderPath,
                IconKind = IconKind.FluentGlyph,
                IconValue = "Document",
                DefaultShell = defaultShell,
                IsPinned = IsPinnedInput.IsChecked ?? true,
                SortOrder = _existing?.SortOrder ?? 0
            };
        }
    }
}
