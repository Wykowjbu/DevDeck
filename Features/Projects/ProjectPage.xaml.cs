using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DevDeck.Contracts;
using DevDeck.Services;
using DevDeck.Models;
using DevDeck.Enums;
using DevDeck.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DevDeck.Features.Projects
{
    public sealed partial class ProjectPage : Page
    {
        private readonly IWorkspaceService _workspaceService;
        private readonly IProjectService _projectService;
        private readonly IActionService _actionService;
        private readonly IActionExecutionService _actionExecutionService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;
        private readonly ISettingsService _settingsService;

        private ProjectEntity? _project;

        public ProjectPage()
        {
            InitializeComponent();
            _workspaceService = App.AppHost.Services.GetRequiredService<IWorkspaceService>();
            _projectService = App.AppHost.Services.GetRequiredService<IProjectService>();
            _actionService = App.AppHost.Services.GetRequiredService<IActionService>();
            _actionExecutionService = App.AppHost.Services.GetRequiredService<IActionExecutionService>();
            _dialogService = App.AppHost.Services.GetRequiredService<IDialogService>();
            _navigationService = App.AppHost.Services.GetRequiredService<INavigationService>();
            _settingsService = App.AppHost.Services.GetRequiredService<ISettingsService>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Guid projectId)
            {
                await LoadProjectAsync(projectId);
            }
        }

        private async Task LoadProjectAsync(Guid projectId)
        {
            var workspaces = await _workspaceService.GetWorkspacesAsync();
            foreach (var ws in workspaces)
            {
                var proj = ws.Projects.FirstOrDefault(p => p.Id == projectId);
                if (proj != null)
                {
                    _project = proj;
                    ProjectNameText.Text = _project.Name;
                    ProjectPathText.Text = _project.FolderPath;
                    ToolTipService.SetToolTip(ProjectPathText, _project.FolderPath);
                    
                    // Update breadcrumb
                    App.MainWindow.SetBreadcrumb($"{ws.Name} / {_project.Name}");

                    // Load Actions for this project
                    await RefreshActionsAsync();
                    return;
                }
            }

            // Project not found
            ProjectNameText.Text = "Không tìm thấy Project";
            ProjectPathText.Text = string.Empty;
        }

        private async Task RefreshActionsAsync()
        {
            if (_project == null) return;

            ApplyActionButtonSize();
            var projectActions = await _actionService.GetProjectActionsAsync(_project.Id);
            if (projectActions == null || projectActions.Count == 0)
            {
                ActionsGridView.Visibility = Visibility.Collapsed;
                NoActionsText.Visibility = Visibility.Visible;
                GroupedActionsSource.Source = null;
            }
            else
            {
                ActionsGridView.Visibility = Visibility.Visible;
                NoActionsText.Visibility = Visibility.Collapsed;

                var grouped = projectActions
                    .GroupBy(pa => pa.GroupNameOverride ?? pa.DevAction?.GroupName ?? "Chung")
                    .OrderBy(g => g.Key == "Chung" ? 1 : 0)
                    .ThenBy(g => g.Key)
                    .ToList();

                GroupedActionsSource.Source = grouped;
            }
        }

        private void ApplyActionButtonSize()
        {
            double width = _settingsService.Settings.ActionButtonSize switch
            {
                ActionButtonSize.Compact => 180,
                ActionButtonSize.Large => 300,
                _ => 240
            };

            var itemStyle = new Style(typeof(GridViewItem));
            itemStyle.Setters.Add(new Setter(FrameworkElement.WidthProperty, width));
            itemStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 54d));
            ActionsGridView.ItemContainerStyle = itemStyle;
        }

        private void OpenFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null || string.IsNullOrEmpty(_project.FolderPath)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _project.FolderPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _ = _dialogService.ShowMessageAsync("Lỗi", $"Không thể mở thư mục: {ex.Message}", XamlRoot);
            }
        }

        private async void EditProject_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null) return;

            var updated = await _dialogService.ShowProjectEditorAsync(_project.WorkspaceId, _project, XamlRoot);
            if (updated != null)
            {
                _project.Name = updated.Name;
                _project.FolderPath = updated.FolderPath;
                _project.DefaultShell = updated.DefaultShell;
                _project.IsPinned = updated.IsPinned;

                await _projectService.UpdateProjectAsync(_project);
                await LoadProjectAsync(_project.Id);

                // Refresh sidebar projects in ShellPage
                if (App.MainWindow.ShellPage is Shell.ShellPage shellPage)
                {
                    await shellPage.RefreshProjectsListAsync();
                }
            }
        }

        private async void AddActionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null) return;

            var allActions = (await _actionService.GetActionsAsync()).ToList();
            var assignedActionIds = _project.ProjectActions.Select(pa => pa.DevActionId).ToHashSet();
            var availableActions = allActions
                .Where(action => !assignedActionIds.Contains(action.Id))
                .OrderBy(action => action.GroupName)
                .ThenBy(action => action.SortOrder)
                .ThenBy(action => action.Name)
                .ToList();

            if (availableActions.Count == 0)
            {
                await _dialogService.ShowMessageAsync(
                    "Không có Action để thêm",
                    "Tất cả Action trong thư viện đã được thêm vào Project này. Bạn có thể tạo Action mới trong trang Actions.",
                    XamlRoot);
                return;
            }

            var listView = new ListView
            {
                ItemsSource = availableActions,
                DisplayMemberPath = nameof(DevActionEntity.Name),
                SelectionMode = ListViewSelectionMode.Multiple,
                MinWidth = 360,
                MaxHeight = 420
            };

            var dialog = new ContentDialog
            {
                Title = "Thêm Action vào Project",
                Content = listView,
                PrimaryButtonText = "Thêm Action",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var selectedActions = listView.SelectedItems.Cast<DevActionEntity>().ToList();
            if (selectedActions.Count == 0)
            {
                return;
            }

            foreach (var action in selectedActions)
            {
                await _actionService.AssignActionToProjectAsync(action.Id, _project.Id);
            }

            await LoadProjectAsync(_project.Id);
        }

        private async void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            if (_project == null) return;

            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Xác nhận xóa Project", 
                $"Bạn có chắc chắn muốn gỡ Project '{_project.Name}' khỏi DevDeck? \nThao tác này chỉ gỡ thông tin hiển thị trên app và sẽ KHÔNG xóa thư mục thực tế trên đĩa cứng.",
                XamlRoot);

            if (confirm)
            {
                await _projectService.DeleteProjectAsync(_project.Id);
                
                // Reset breadcrumb
                App.MainWindow.SetBreadcrumb(string.Empty);

                // Refresh sidebar projects in ShellPage
                if (App.MainWindow.ShellPage is Shell.ShellPage shellPage)
                {
                    await shellPage.RefreshProjectsListAsync();
                }

                // Go back to Home
                _navigationService.Navigate(typeof(Home.HomePage));
            }
        }

        private async void ActionButtonControl_RunClicked(object sender, ProjectActionEntity e)
        {
            if (sender is ActionButtonControl control)
            {
                await _actionExecutionService.RunProjectActionAsync(e, XamlRoot, state =>
                {
                    control.SetState(state);
                });
            }
        }

        private async void ActionButtonControl_StopClicked(object sender, ProjectActionEntity e)
        {
            if (sender is ActionButtonControl control)
            {
                await _actionExecutionService.StopProjectActionAsync(e.ProjectId, e.DevActionId);
                control.SetState(RunState.Stopped);
            }
        }

        private async void ActionButtonControl_RemoveClicked(object sender, ProjectActionEntity e)
        {
            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Xác nhận gỡ Action", 
                $"Bạn có chắc chắn muốn gỡ Action '{e.NameOverride ?? e.DevAction?.Name}' khỏi Project này? \nHành động này không xóa Action gốc.",
                XamlRoot);

            if (confirm && _project != null)
            {
                await _actionService.RemoveActionFromProjectAsync(e.DevActionId, _project.Id);
                await RefreshActionsAsync();
            }
        }

        private async void ActionButtonControl_CustomizeClicked(object sender, ProjectActionEntity e)
        {
            var updated = await _dialogService.ShowProjectActionOverrideAsync(e, XamlRoot);
            if (updated != null)
            {
                await _actionService.UpdateProjectActionAsync(updated);
                await RefreshActionsAsync();
            }
        }

        private async void ActionButtonControl_ResetClicked(object sender, ProjectActionEntity e)
        {
            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Xác nhận Reset", 
                $"Bạn có chắc chắn muốn xóa mọi tùy chỉnh của Action '{e.NameOverride ?? e.DevAction?.Name}' trên Project này và quay về mặc định?",
                XamlRoot);

            if (confirm)
            {
                // Clear overrides
                e.NameOverride = null;
                e.IconKindOverride = null;
                e.IconValueOverride = null;
                e.GroupNameOverride = null;
                e.RequireConfirmationOverride = null;
                e.StopOnFailureOverride = null;
                e.AllowConcurrentRunsOverride = null;
                e.StepOverrides.Clear();

                await _actionService.UpdateProjectActionAsync(e);
                await RefreshActionsAsync();
            }
        }
    }
}
