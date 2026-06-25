using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Contracts;
using DevDeck.Enums;
using DevDeck.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace DevDeck.Features.Actions
{
    public sealed partial class ActionEditorControl : UserControl
    {
        private readonly IActionService _actionService;
        private readonly IDialogService _dialogService;
        private readonly ObservableCollection<ActionStepEntity> _steps = [];

        private DevActionEntity? _selectedAction;
        private DevActionEntity? _draftAction;
        private bool _isDirty;
        private bool _isNewDraft;
        private bool _isLoadingEditor;

        public event EventHandler<Guid>? Saved;
        public event EventHandler? Deleted;

        public bool HasUnsavedChanges => _isDirty;

        public ActionEditorControl()
        {
            InitializeComponent();
            _actionService = App.AppHost.Services.GetRequiredService<IActionService>();
            _dialogService = App.AppHost.Services.GetRequiredService<IDialogService>();
            StepsListView.ItemsSource = _steps;
        }

        public void LoadAction(DevActionEntity action, bool isNewDraft)
        {
            _isLoadingEditor = true;
            _selectedAction = isNewDraft ? null : action;
            _draftAction = CloneAction(action);
            _isNewDraft = isNewDraft;

            EditorTitleText.Text = isNewDraft ? "New Action" : action.Name;
            DeleteButton.IsEnabled = !isNewDraft;

            ActionNameInput.Text = _draftAction.Name;
            GroupNameInput.Text = _draftAction.GroupName ?? string.Empty;
            RequireConfirmInput.IsChecked = _draftAction.RequireConfirmation;
            StopOnFailureInput.IsChecked = _draftAction.StopOnFailure;

            _steps.Clear();
            foreach (var step in _draftAction.Steps.OrderBy(s => s.SortOrder))
            {
                _steps.Add(CloneStep(step));
            }

            var selectedStep = _steps.FirstOrDefault();
            StepsListView.SelectedItem = selectedStep;
            _isLoadingEditor = false;
            LoadStepIntoEditor(selectedStep);
            ResetHorizontalViewport();
            SetDirty(false);
        }

        public void Clear()
        {
            _selectedAction = null;
            _draftAction = null;
            _isNewDraft = false;
            _steps.Clear();
            LoadStepIntoEditor(null);
            SetDirty(false);
        }

        public void MarkDirty()
        {
            SetDirty(true);
        }

        public async Task<bool> ConfirmLeaveAsync()
        {
            if (!HasUnsavedChanges) return true;

            var dialog = new ContentDialog
            {
                Title = "Lưu thay đổi Action?",
                Content = "Action hiện tại có thay đổi chưa lưu. Bạn muốn lưu, bỏ thay đổi, hay tiếp tục chỉnh sửa?",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Discard",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                return await SaveDraftAsync();
            }

            if (result == ContentDialogResult.Secondary)
            {
                SetDirty(false);
                return true;
            }

            return false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveDraftAsync();
        }

        private async Task<bool> SaveDraftAsync()
        {
            if (_draftAction == null) return true;

            string name = ActionNameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                await _dialogService.ShowMessageAsync("Thiếu tên Action", "Tên Action không được để trống.", XamlRoot);
                ActionNameInput.Focus(FocusState.Programmatic);
                return false;
            }

            if (_steps.Count == 0)
            {
                await _dialogService.ShowMessageAsync("Thiếu Step", "Action phải có ít nhất 1 step.", XamlRoot);
                return false;
            }

            foreach (var step in _steps)
            {
                if (step.StepType == ActionStepType.ShellCommand && string.IsNullOrWhiteSpace(step.CommandText))
                {
                    await _dialogService.ShowMessageAsync("Thiếu lệnh Command", "Shell step phải có lệnh Command.", XamlRoot);
                    StepsListView.SelectedItem = step;
                    CommandTextInput.Focus(FocusState.Programmatic);
                    return false;
                }
            }

            _draftAction.Name = name;
            _draftAction.GroupName = string.IsNullOrWhiteSpace(GroupNameInput.Text) ? null : GroupNameInput.Text.Trim();
            _draftAction.RequireConfirmation = RequireConfirmInput.IsChecked ?? false;
            _draftAction.StopOnFailure = StopOnFailureInput.IsChecked ?? true;
            _draftAction.Steps = _steps.Select(CloneStep).ToList();

            Guid savedId;
            if (_isNewDraft)
            {
                var created = await _actionService.CreateActionAsync(_draftAction);
                savedId = created.Id;
            }
            else if (_selectedAction != null)
            {
                _draftAction.Id = _selectedAction.Id;
                _draftAction.SortOrder = _selectedAction.SortOrder;
                await _actionService.UpdateActionAsync(_draftAction);
                savedId = _draftAction.Id;
            }
            else
            {
                return true;
            }

            SetDirty(false);
            Saved?.Invoke(this, savedId);
            return true;
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isNewDraft)
            {
                Clear();
                Deleted?.Invoke(this, EventArgs.Empty);
            }
            else if (_selectedAction != null)
            {
                LoadAction(_selectedAction, isNewDraft: false);
            }
        }

        private async void DeleteActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAction == null) return;

            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Xác nhận xóa Action",
                $"Bạn có chắc chắn muốn xóa Action '{_selectedAction.Name}' ra khỏi thư viện? Thao tác này cũng gỡ Action khỏi tất cả Project đã gán.",
                XamlRoot);

            if (!confirm) return;

            await _actionService.DeleteActionAsync(_selectedAction.Id);
            Clear();
            Deleted?.Invoke(this, EventArgs.Empty);
        }

        private void EditorFieldChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingEditor) return;
            SetDirty(true);
        }

        private void EditorCheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_isLoadingEditor) return;
            SetDirty(true);
        }

        private void StepsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingEditor) return;
            LoadStepIntoEditor(StepsListView.SelectedItem as ActionStepEntity);
        }

        private void LoadStepIntoEditor(ActionStepEntity? step)
        {
            if (step == null)
            {
                NoStepSelectedText.Visibility = Visibility.Visible;
                StepDetailPanel.Visibility = Visibility.Collapsed;
                return;
            }

            _isLoadingEditor = true;
            NoStepSelectedText.Visibility = Visibility.Collapsed;
            StepDetailPanel.Visibility = Visibility.Visible;
            StepTypeText.Text = $"Bước {step.SortOrder}: {step.StepType}";

            if (step.StepType == ActionStepType.ShellCommand)
            {
                ShellStepPanel.Visibility = Visibility.Visible;
                DelayStepPanel.Visibility = Visibility.Collapsed;

                CommandTextInput.Text = step.CommandText ?? string.Empty;
                ShellInput.SelectedIndex = step.Shell switch
                {
                    ShellType.PowerShell7 => 0,
                    ShellType.WindowsPowerShell => 1,
                    ShellType.CommandPrompt => 2,
                    ShellType.GitBash => 3,
                    _ => 2
                };
                OutputModeInput.SelectedIndex = step.OutputMode == ActionOutputMode.ExternalTerminal ? 1 : 0;
            }
            else
            {
                ShellStepPanel.Visibility = Visibility.Collapsed;
                DelayStepPanel.Visibility = Visibility.Visible;
                DelayInput.Value = step.DelayMilliseconds ?? 1000;
            }

            _isLoadingEditor = false;
        }

        private void AddShellStep_Click(object sender, RoutedEventArgs e)
        {
            var step = new ActionStepEntity
            {
                Id = Guid.NewGuid(),
                StepType = ActionStepType.ShellCommand,
                SortOrder = _steps.Count + 1,
                CommandText = "echo Hello World",
                Shell = ShellType.CommandPrompt,
                OutputMode = ActionOutputMode.Silent,
                WorkingDirectory = "${project.path}",
                StopOnFailure = true
            };
            _steps.Add(step);
            StepsListView.SelectedItem = step;
            LoadStepIntoEditor(step);
            ResetHorizontalViewport();
            SetDirty(true);
        }

        private void AddDelayStep_Click(object sender, RoutedEventArgs e)
        {
            var step = new ActionStepEntity
            {
                Id = Guid.NewGuid(),
                StepType = ActionStepType.Delay,
                SortOrder = _steps.Count + 1,
                DelayMilliseconds = 1000,
                WorkingDirectory = "${project.path}",
                StopOnFailure = true
            };
            _steps.Add(step);
            StepsListView.SelectedItem = step;
            LoadStepIntoEditor(step);
            ResetHorizontalViewport();
            SetDirty(true);
        }

        private void RemoveStep_Click(object sender, RoutedEventArgs e)
        {
            if (StepsListView.SelectedItem is not ActionStepEntity step) return;

            int index = _steps.IndexOf(step);
            _isLoadingEditor = true;
            _steps.Remove(step);
            ResequenceSteps();
            int nextIndex = _steps.Count == 0 ? -1 : Math.Min(index, _steps.Count - 1);
            StepsListView.SelectedIndex = nextIndex;
            var nextStep = nextIndex >= 0 ? _steps[nextIndex] : null;
            _isLoadingEditor = false;
            LoadStepIntoEditor(nextStep);
            ResetHorizontalViewport();
            SetDirty(true);
        }

        private void CommandTextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingEditor) return;
            if (StepsListView.SelectedItem is ActionStepEntity step && step.StepType == ActionStepType.ShellCommand)
            {
                step.CommandText = CommandTextInput.Text;
                SetDirty(true);
            }
        }

        private void ShellInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingEditor) return;
            if (StepsListView.SelectedItem is ActionStepEntity step && step.StepType == ActionStepType.ShellCommand)
            {
                step.Shell = ShellInput.SelectedIndex switch
                {
                    0 => ShellType.PowerShell7,
                    1 => ShellType.WindowsPowerShell,
                    2 => ShellType.CommandPrompt,
                    3 => ShellType.GitBash,
                    _ => ShellType.CommandPrompt
                };
                SetDirty(true);
            }
        }

        private void OutputModeInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingEditor) return;
            if (StepsListView.SelectedItem is ActionStepEntity step && step.StepType == ActionStepType.ShellCommand)
            {
                step.OutputMode = OutputModeInput.SelectedIndex == 1
                    ? ActionOutputMode.ExternalTerminal
                    : ActionOutputMode.Silent;
                SetDirty(true);
            }
        }

        private void DelayInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isLoadingEditor) return;
            if (StepsListView.SelectedItem is ActionStepEntity step && step.StepType == ActionStepType.Delay)
            {
                step.DelayMilliseconds = (int)Math.Clamp(sender.Value, 1, int.MaxValue);
                SetDirty(true);
            }
        }

        private void SetDirty(bool isDirty)
        {
            _isDirty = isDirty;
            SaveButton.IsEnabled = isDirty;
            DiscardButton.IsEnabled = isDirty;
            UnsavedText.Visibility = isDirty ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ResetHorizontalViewport()
        {
            DispatcherQueue.TryEnqueue(() => EditorScrollViewer.ChangeView(0, null, null, true));
        }

        private void ResequenceSteps()
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                _steps[i].SortOrder = i + 1;
            }
        }

        private static DevActionEntity CloneAction(DevActionEntity action)
        {
            return new DevActionEntity
            {
                Id = action.Id,
                Name = action.Name,
                IconKind = action.IconKind,
                IconValue = action.IconValue,
                Scope = action.Scope,
                OwnerWorkspaceId = action.OwnerWorkspaceId,
                OwnerProjectId = action.OwnerProjectId,
                GroupName = action.GroupName,
                SortOrder = action.SortOrder,
                ExecutionMode = action.ExecutionMode,
                StopOnFailure = action.StopOnFailure,
                RequireConfirmation = action.RequireConfirmation,
                AllowConcurrentRuns = action.AllowConcurrentRuns,
                Steps = action.Steps.OrderBy(s => s.SortOrder).Select(CloneStep).ToList()
            };
        }

        private static ActionStepEntity CloneStep(ActionStepEntity step)
        {
            return new ActionStepEntity
            {
                Id = step.Id == Guid.Empty ? Guid.NewGuid() : step.Id,
                DevActionId = step.DevActionId,
                StepType = step.StepType,
                SortOrder = step.SortOrder,
                CommandText = step.CommandText,
                ApplicationPath = step.ApplicationPath,
                Arguments = step.Arguments,
                TargetPath = step.TargetPath,
                Url = step.Url,
                WorkingDirectory = step.WorkingDirectory,
                Shell = step.Shell,
                OutputMode = step.OutputMode == ActionOutputMode.IntegratedTerminal
                    ? ActionOutputMode.ExternalTerminal
                    : step.OutputMode,
                DelayMilliseconds = step.DelayMilliseconds,
                StopOnFailure = step.StopOnFailure
            };
        }
    }
}
