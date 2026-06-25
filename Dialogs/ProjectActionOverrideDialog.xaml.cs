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
    public sealed partial class ProjectActionOverrideDialog : ContentDialog
    {
        public ProjectActionEntity? Result { get; private set; }
        private readonly ProjectActionEntity _existing;
        private readonly List<StepOverrideViewModel> _steps = [];
        private bool _isUpdatingFields = false;

        public ProjectActionOverrideDialog(ProjectActionEntity existing)
        {
            InitializeComponent();
            _existing = existing;

            Title = $"Tùy chỉnh \"{_existing.DevAction?.Name}\"";
            DialogSubheaderText.Text = $"Thay đổi ở đây chỉ áp dụng cho Project: {_existing.Project?.Name}";

            NameOverrideInput.Text = _existing.NameOverride ?? string.Empty;
            GroupNameOverrideInput.Text = _existing.GroupNameOverride ?? string.Empty;

            // Set combobox selections
            RequireConfirmationInput.SelectedIndex = _existing.RequireConfirmationOverride switch
            {
                null => 0,
                true => 1,
                false => 2
            };

            StopOnFailureInput.SelectedIndex = _existing.StopOnFailureOverride switch
            {
                null => 0,
                true => 1,
                false => 2
            };

            // Build steps list
            if (_existing.DevAction != null)
            {
                int index = 1;
                foreach (var step in _existing.DevAction.Steps.OrderBy(s => s.SortOrder))
                {
                    var over = _existing.StepOverrides.FirstOrDefault(o => o.ActionStepId == step.Id);
                    _steps.Add(new StepOverrideViewModel
                    {
                        StepId = step.Id,
                        Index = index++,
                        StepType = step.StepType,
                        OriginalCommand = step.CommandText ?? string.Empty,
                        CommandOverride = over?.CommandTextOverride ?? string.Empty,
                        WorkingDirectoryOverride = over?.WorkingDirectoryOverride ?? string.Empty
                    });
                }
            }

            StepsListView.ItemsSource = _steps;
            PrimaryButtonClick += ProjectActionOverrideDialog_PrimaryButtonClick;
        }

        private void StepsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StepsListView.SelectedItem is StepOverrideViewModel step)
            {
                _isUpdatingFields = true;
                StepDetailPanel.Visibility = Visibility.Visible;
                StepTypeText.Text = $"Bước {step.Index}: {step.StepType}";

                if (step.StepType == ActionStepType.ShellCommand)
                {
                    CommandTextOverrideInput.Visibility = Visibility.Visible;
                    CommandTextOverrideInput.Text = step.CommandOverride;
                }
                else
                {
                    CommandTextOverrideInput.Visibility = Visibility.Collapsed;
                }

                WorkingDirectoryOverrideInput.Text = step.WorkingDirectoryOverride;
                _isUpdatingFields = false;
            }
            else
            {
                StepDetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void CommandTextOverrideInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFields) return;
            if (StepsListView.SelectedItem is StepOverrideViewModel step)
            {
                step.CommandOverride = CommandTextOverrideInput.Text;
            }
        }

        private void WorkingDirectoryOverrideInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingFields) return;
            if (StepsListView.SelectedItem is StepOverrideViewModel step)
            {
                step.WorkingDirectoryOverride = WorkingDirectoryOverrideInput.Text;
            }
        }

        private void ProjectActionOverrideDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Build result
            var result = new ProjectActionEntity
            {
                ProjectId = _existing.ProjectId,
                DevActionId = _existing.DevActionId,
                NameOverride = string.IsNullOrEmpty(NameOverrideInput.Text.Trim()) ? null : NameOverrideInput.Text.Trim(),
                GroupNameOverride = string.IsNullOrEmpty(GroupNameOverrideInput.Text.Trim()) ? null : GroupNameOverrideInput.Text.Trim(),
                IconKindOverride = _existing.IconKindOverride, // keep icons as they are for now
                IconValueOverride = _existing.IconValueOverride,
                RequireConfirmationOverride = RequireConfirmationInput.SelectedIndex switch
                {
                    1 => true,
                    2 => false,
                    _ => null
                },
                StopOnFailureOverride = StopOnFailureInput.SelectedIndex switch
                {
                    1 => true,
                    2 => false,
                    _ => null
                },
                AllowConcurrentRunsOverride = _existing.AllowConcurrentRunsOverride
            };

            // Map step overrides back to entities
            foreach (var vm in _steps)
            {
                if (!string.IsNullOrEmpty(vm.CommandOverride?.Trim()) || !string.IsNullOrEmpty(vm.WorkingDirectoryOverride?.Trim()))
                {
                    result.StepOverrides.Add(new ProjectActionStepOverrideEntity
                    {
                        ProjectId = _existing.ProjectId,
                        DevActionId = _existing.DevActionId,
                        ActionStepId = vm.StepId,
                        CommandTextOverride = string.IsNullOrEmpty(vm.CommandOverride?.Trim()) ? null : vm.CommandOverride.Trim(),
                        WorkingDirectoryOverride = string.IsNullOrEmpty(vm.WorkingDirectoryOverride?.Trim()) ? null : vm.WorkingDirectoryOverride.Trim()
                    });
                }
            }

            Result = result;
        }

        public class StepOverrideViewModel
        {
            public Guid StepId { get; set; }
            public int Index { get; set; }
            public ActionStepType StepType { get; set; }
            public string OriginalCommand { get; set; } = string.Empty;
            public string CommandOverride { get; set; } = string.Empty;
            public string WorkingDirectoryOverride { get; set; } = string.Empty;

            public string DisplayText => $"Bước {Index} ({StepType})";
        }
    }
}
