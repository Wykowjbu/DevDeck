using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Contracts;
using DevDeck.Enums;
using DevDeck.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevDeck.Features.Actions
{
    public sealed partial class ActionsPage : Page, IUnsavedChangesGuard
    {
        private readonly IActionService _actionService;
        private List<DevActionEntity> _allActions = [];
        private DevActionEntity? _selectedAction;
        private bool _isChangingSelection;

        public bool HasUnsavedChanges => EditorControl.HasUnsavedChanges;

        public ActionsPage()
        {
            InitializeComponent();
            _actionService = App.AppHost.Services.GetRequiredService<IActionService>();
            Loaded += ActionsPage_Loaded;
        }

        private async void ActionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadActionsListAsync();
        }

        private async Task LoadActionsListAsync(Guid? selectActionId = null)
        {
            var actions = await _actionService.GetActionsAsync();
            _allActions = actions.ToList();
            FilterActions();

            if (selectActionId.HasValue)
            {
                var toSelect = _allActions.FirstOrDefault(a => a.Id == selectActionId.Value);
                if (toSelect != null)
                {
                    _isChangingSelection = true;
                    ActionsListView.SelectedItem = toSelect;
                    _isChangingSelection = false;
                    ShowEditor(toSelect, isNewDraft: false);
                }
            }
        }

        private void FilterActions()
        {
            string query = SearchBox.Text.Trim().ToLowerInvariant();
            ActionsListView.ItemsSource = string.IsNullOrEmpty(query)
                ? _allActions
                : _allActions.Where(a => a.Name.ToLowerInvariant().Contains(query)).ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterActions();
        }

        private async void ActionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isChangingSelection) return;

            var nextAction = ActionsListView.SelectedItem as DevActionEntity;
            if (nextAction == _selectedAction) return;

            if (!await ConfirmLeaveAsync())
            {
                _isChangingSelection = true;
                ActionsListView.SelectedItem = _selectedAction;
                _isChangingSelection = false;
                return;
            }

            if (nextAction != null)
            {
                ShowEditor(nextAction, isNewDraft: false);
            }
            else
            {
                HideEditor();
            }
        }

        private async void NewActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!await ConfirmLeaveAsync())
            {
                return;
            }

            var draft = new DevActionEntity
            {
                Id = Guid.Empty,
                Name = "New Action",
                IconKind = IconKind.FluentGlyph,
                IconValue = "Play",
                Scope = ActionScope.Global,
                RequireConfirmation = false,
                StopOnFailure = true,
                AllowConcurrentRuns = false,
                ExecutionMode = ActionExecutionMode.Sequential,
                Steps =
                [
                    new ActionStepEntity
                    {
                        Id = Guid.NewGuid(),
                        StepType = ActionStepType.ShellCommand,
                        SortOrder = 1,
                        CommandText = "echo Hello World",
                        Shell = ShellType.CommandPrompt,
                        OutputMode = ActionOutputMode.Silent,
                        WorkingDirectory = "${project.path}",
                        StopOnFailure = true
                    }
                ]
            };

            _isChangingSelection = true;
            ActionsListView.SelectedItem = null;
            _isChangingSelection = false;
            ShowEditor(draft, isNewDraft: true);
            EditorControl.MarkDirty();
        }

        private void ShowEditor(DevActionEntity action, bool isNewDraft)
        {
            _selectedAction = isNewDraft ? null : action;
            EmptyStateText.Visibility = Visibility.Collapsed;
            EditorControl.Visibility = Visibility.Visible;
            EditorControl.LoadAction(action, isNewDraft);
        }

        private void HideEditor()
        {
            _selectedAction = null;
            EditorControl.Clear();
            EditorControl.Visibility = Visibility.Collapsed;
            EmptyStateText.Visibility = Visibility.Visible;
        }

        private async void EditorControl_Saved(object sender, Guid savedActionId)
        {
            await LoadActionsListAsync(savedActionId);
        }

        private async void EditorControl_Deleted(object sender, EventArgs e)
        {
            _isChangingSelection = true;
            ActionsListView.SelectedItem = null;
            _isChangingSelection = false;
            HideEditor();
            await LoadActionsListAsync();

            if (App.MainWindow.ShellPage is Shell.ShellPage shellPage)
            {
                await shellPage.RefreshProjectsListAsync();
            }
        }

        public async Task<bool> ConfirmLeaveAsync()
        {
            return await EditorControl.ConfirmLeaveAsync();
        }
    }
}
