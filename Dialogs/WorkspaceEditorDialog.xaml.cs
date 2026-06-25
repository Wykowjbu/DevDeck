using Microsoft.UI.Xaml.Controls;
using DevDeck.Models;
using DevDeck.Enums;
using System;

namespace DevDeck.Dialogs
{
    public sealed partial class WorkspaceEditorDialog : ContentDialog
    {
        public WorkspaceEntity? WorkspaceResult { get; private set; }
        private readonly WorkspaceEntity? _existing;

        public WorkspaceEditorDialog(WorkspaceEntity? existing)
        {
            InitializeComponent();
            _existing = existing;

            if (_existing != null)
            {
                Title = "Chỉnh sửa Workspace";
                WorkspaceNameInput.Text = _existing.Name;
                
                // Select accent color
                int index = _existing.AccentColor switch
                {
                    "Blue" => 1,
                    "Green" => 2,
                    "Red" => 3,
                    "Purple" => 4,
                    "Orange" => 5,
                    _ => 0
                };
                AccentColorInput.SelectedIndex = index;
            }
            else
            {
                Title = "Tạo Workspace Mới";
            }

            PrimaryButtonClick += WorkspaceEditorDialog_PrimaryButtonClick;
        }

        private void WorkspaceEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string name = WorkspaceNameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                args.Cancel = true;
                WorkspaceNameInput.Header = "Tên Workspace (Không được để trống)";
                return;
            }

            string? accentColor = (AccentColorInput.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (accentColor == "Mặc định") accentColor = null;

            WorkspaceResult = new WorkspaceEntity
            {
                Id = _existing?.Id ?? Guid.Empty,
                Name = name,
                IconKind = IconKind.FluentGlyph,
                IconValue = "Folder",
                AccentColor = accentColor,
                SortOrder = _existing?.SortOrder ?? 0
            };
        }
    }
}
