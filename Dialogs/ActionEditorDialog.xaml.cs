using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Models;
using DevDeck.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DevDeck.Dialogs
{
    public sealed partial class ActionEditorDialog : ContentDialog
    {
        public DevActionEntity? ActionResult { get; private set; }
        private readonly DevActionEntity? _existing;
        private readonly ObservableCollection<ActionStepEntity> _steps = [];
        private bool _isUpdatingFields = false;

        public ActionEditorDialog(DevActionEntity? existing)
        {
            InitializeComponent();
            _existing = existing;

            StepsListView.ItemsSource = _steps;

            if (_existing != null)
            {
                Title = "Chỉnh sửa Action";
                ActionNameInput.Text = _existing.Name;
                GroupNameInput.Text = _existing.GroupName;
                RequireConfirmInput.IsChecked = _existing.RequireConfirmation;
                StopOnFailureInput.IsChecked = _existing.StopOnFailure;

                foreach (var step in _existing.Steps.OrderBy(s => s.SortOrder))
                {
                    _steps.Add(new ActionStepEntity
                    {
                        Id = step.Id,
                        DevActionId = step.DevActionId,
                        StepType = step.StepType,
                        SortOrder = step.SortOrder,
                        CommandText = step.CommandText,
                        DelayMilliseconds = step.DelayMilliseconds,
                        Shell = step.Shell,
                        OutputMode = step.OutputMode == ActionOutputMode.IntegratedTerminal
                            ? ActionOutputMode.ExternalTerminal
                            : step.OutputMode
                    });
                }
            }
            else
            {
                Title = "Tạo Action Mới";
            }

            PrimaryButtonClick += ActionEditorDialog_PrimaryButtonClick;
        }

        private void StepsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StepsListView.SelectedItem is ActionStepEntity step)
            {
                _isUpdatingFields = true;
                StepDetailPanel.Visibility = Visibility.Visible;
                StepTypeText.Text = $"Cấu hình bước: {step.StepType}";

                if (step.StepType == ActionStepType.ShellCommand)
                {
                    CommandTextInput.Visibility = Visibility.Visible;
                    ShellInput.Visibility = Visibility.Visible;
                    OutputModeInput.Visibility = Visibility.Visible;
                    DelayInput.Visibility = Visibility.Collapsed;
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
                else if (step.StepType == ActionStepType.Delay)
                {
                    CommandTextInput.Visibility = Visibility.Collapsed;
                    ShellInput.Visibility = Visibility.Collapsed;
                    OutputModeInput.Visibility = Visibility.Collapsed;
                    DelayInput.Visibility = Visibility.Visible;
                    DelayInput.Value = step.DelayMilliseconds ?? 1000;
                }
                _isUpdatingFields = false;
            }
            else
            {
                StepDetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void AddShellStep_Click(object sender, RoutedEventArgs e)
        {
            var step = new ActionStepEntity
            {
                Id = Guid.NewGuid(),
                StepType = ActionStepType.ShellCommand,
                CommandText = "echo 'Hello World'",
                SortOrder = _steps.Count + 1,
                Shell = ShellType.CommandPrompt,
                OutputMode = ActionOutputMode.Silent
            };
            _steps.Add(step);
            StepsListView.SelectedItem = step;
        }

        private void AddDelayStep_Click(object sender, RoutedEventArgs e)
        {
            var step = new ActionStepEntity
            {
                Id = Guid.NewGuid(),
                StepType = ActionStepType.Delay,
                DelayMilliseconds = 1000,
                SortOrder = _steps.Count + 1
            };
            _steps.Add(step);
            StepsListView.SelectedItem = step;
        }

        private void RemoveStep_Click(object sender, RoutedEventArgs e)
        {
            if (StepsListView.SelectedItem is ActionStepEntity step)
            {
                _steps.Remove(step);
                
                // Recalculate sort order
                for (int i = 0; i < _steps.Count; i++)
                {
                    _steps[i].SortOrder = i + 1;
                }
            }
        }

        private void CommandTextInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFields) return;
            if (StepsListView.SelectedItem is ActionStepEntity step && step.StepType == ActionStepType.ShellCommand)
            {
                step.CommandText = CommandTextInput.Text;
            }
        }

        private void ShellInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFields) return;
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
            }
        }

        private void OutputModeInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingFields) return;
            if (StepsListView.SelectedItem is ActionStepEntity step && step.StepType == ActionStepType.ShellCommand)
            {
                step.OutputMode = OutputModeInput.SelectedIndex == 1
                    ? ActionOutputMode.ExternalTerminal
                    : ActionOutputMode.Silent;
            }
        }

        private void DelayInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isUpdatingFields) return;
            if (StepsListView.SelectedItem is ActionStepEntity step && step.StepType == ActionStepType.Delay)
            {
                step.DelayMilliseconds = (int)sender.Value;
            }
        }

        private void ActionEditorDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string name = ActionNameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                args.Cancel = true;
                ShowValidationError("Tên Action không được để trống.", ActionNameInput);
                return;
            }

            if (_steps.Count == 0)
            {
                args.Cancel = true;
                ShowValidationError("Action phải có ít nhất 1 bước chạy.", StepsListView);
                return;
            }

            // Validate step commands
            foreach (var step in _steps)
            {
                if (step.StepType == ActionStepType.ShellCommand && string.IsNullOrEmpty(step.CommandText?.Trim()))
                {
                    args.Cancel = true;
                    ShowValidationError("Lệnh Command của các bước chạy không được để trống.", StepsListView);
                    return;
                }
            }

            ActionResult = new DevActionEntity
            {
                Id = _existing?.Id ?? Guid.Empty,
                Name = name,
                IconKind = IconKind.FluentGlyph,
                IconValue = "Play",
                Scope = _existing?.Scope ?? ActionScope.Global,
                GroupName = string.IsNullOrEmpty(GroupNameInput.Text.Trim()) ? null : GroupNameInput.Text.Trim(),
                RequireConfirmation = RequireConfirmInput.IsChecked ?? false,
                StopOnFailure = StopOnFailureInput.IsChecked ?? true,
                AllowConcurrentRuns = _existing?.AllowConcurrentRuns ?? false,
                Steps = _steps.ToList(),
                SortOrder = _existing?.SortOrder ?? 0
            };
        }

        private void ShowValidationError(string message, Control focusTarget)
        {
            ValidationErrorText.Text = message;
            ValidationErrorText.Visibility = Visibility.Visible;
            focusTarget.Focus(FocusState.Programmatic);
        }
    }
}
