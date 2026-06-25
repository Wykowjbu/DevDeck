using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Contracts;
using DevDeck.Services;
using DevDeck.Features.Home;
using DevDeck.Features.Actions;
using DevDeck.Features.Settings;
using DevDeck.Features.Projects;
using DevDeck.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;

namespace DevDeck.Shell
{
    public sealed partial class ShellPage : Page
    {
        private readonly INavigationService _navigationService;
        private readonly AppStateService _appStateService;
        private readonly IWorkspaceService _workspaceService;
        private readonly IProjectService _projectService;
        private readonly IDialogService _dialogService;

        private bool _isUpdatingSelection = false;
        private NavigationViewItem? _lastSelectedNavigationItem;

        public ShellPage(
            INavigationService navigationService, 
            AppStateService appStateService,
            IWorkspaceService workspaceService,
            IProjectService projectService,
            IDialogService dialogService)
        {
            InitializeComponent();
            _navigationService = navigationService;
            _appStateService = appStateService;
            _workspaceService = workspaceService;
            _projectService = projectService;
            _dialogService = dialogService;

            _navigationService.Initialize(ContentFrame);
            
            // Navigate to Home initially
            _navigationService.Navigate(typeof(HomePage));
            NavigationControl.SelectedItem = HomeMenuItem;
            _lastSelectedNavigationItem = HomeMenuItem;

            // Setup keyboard accelerators programmatically
            var settingsAcc = new KeyboardAccelerator
            {
                Key = Windows.System.VirtualKey.P,
                Modifiers = Windows.System.VirtualKeyModifiers.Control
            };
            settingsAcc.Invoked += Settings_Accelerator;
            this.KeyboardAccelerators.Add(settingsAcc);

            // Load data
            _ = InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            await LoadWorkspacesAsync();
        }

        private async Task LoadWorkspacesAsync(Guid? selectWorkspaceId = null)
        {
            _isUpdatingSelection = true;
            var workspaces = await _workspaceService.GetWorkspacesAsync();
            WorkspaceSelector.ItemsSource = workspaces;
            _isUpdatingSelection = false;
            
            if (workspaces.Count > 0)
            {
                if (selectWorkspaceId.HasValue)
                {
                    var toSelect = workspaces.FirstOrDefault(w => w.Id == selectWorkspaceId.Value);
                    WorkspaceSelector.SelectedItem = toSelect ?? workspaces[0];
                }
                else
                {
                    WorkspaceSelector.SelectedIndex = 0;
                }
            }
            else
            {
                WorkspaceSelector.SelectedItem = null;
                _appStateService.CurrentWorkspaceId = null;
                ClearProjectsList();
            }
        }

        private void ClearProjectsList()
        {
            while (NavigationControl.MenuItems.Count > 3)
            {
                NavigationControl.MenuItems.RemoveAt(3);
            }
        }

        private async Task LoadProjectsAsync(Guid workspaceId)
        {
            ClearProjectsList();

            var projects = await _projectService.GetProjectsAsync(workspaceId);
            foreach (var project in projects)
            {
                var item = new NavigationViewItem
                {
                    Content = project.Name,
                    Icon = new SymbolIcon(Symbol.Document),
                    Tag = project.Id
                };
                NavigationControl.MenuItems.Add(item);
            }

            // Add "Add Project..." item
            var addProjectItem = new NavigationViewItem
            {
                Content = "Thêm Project...",
                Icon = new SymbolIcon(Symbol.Add),
                Tag = "ADD_PROJECT"
            };
            NavigationControl.MenuItems.Add(addProjectItem);
        }

        private async void WorkspaceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection) return;

            if (WorkspaceSelector.SelectedItem is WorkspaceEntity workspace)
            {
                _appStateService.CurrentWorkspaceId = workspace.Id;
                await LoadProjectsAsync(workspace.Id);

                // Reload HomePage if active
                if (NavigationControl.SelectedItem is NavigationViewItem selectedItem && selectedItem == HomeMenuItem)
                {
                    _navigationService.Navigate(typeof(HomePage));
                }
            }
        }

        private async void NewWorkspace_Click(object sender, RoutedEventArgs e)
        {
            var ws = await _dialogService.ShowWorkspaceEditorAsync(null, XamlRoot);
            if (ws != null)
            {
                var created = await _workspaceService.CreateWorkspaceAsync(ws.Name, ws.IconKind, ws.IconValue, ws.AccentColor);
                await LoadWorkspacesAsync(created.Id);
            }
        }

        private async void RenameWorkspace_Click(object sender, RoutedEventArgs e)
        {
            if (WorkspaceSelector.SelectedItem is WorkspaceEntity workspace)
            {
                var ws = await _dialogService.ShowWorkspaceEditorAsync(workspace, XamlRoot);
                if (ws != null)
                {
                    workspace.Name = ws.Name;
                    workspace.AccentColor = ws.AccentColor;
                    await _workspaceService.UpdateWorkspaceAsync(workspace);
                    await LoadWorkspacesAsync(workspace.Id);
                }
            }
        }

        private async void DeleteWorkspace_Click(object sender, RoutedEventArgs e)
        {
            if (WorkspaceSelector.SelectedItem is WorkspaceEntity workspace)
            {
                int projectCount = workspace.Projects.Count;
                string message = $"Bạn có chắc chắn muốn xóa Workspace '{workspace.Name}'? \nViệc xóa Workspace này sẽ không xóa các thư mục dự án thực tế trên đĩa. ";
                if (projectCount > 0)
                {
                    message += $"\nCó {projectCount} dự án trong DevDeck sẽ bị ảnh hưởng (bị gỡ khỏi danh sách).";
                }

                bool confirm = await _dialogService.ShowConfirmationAsync("Xác nhận xóa Workspace", message, XamlRoot);
                if (confirm)
                {
                    await _workspaceService.DeleteWorkspaceAsync(workspace.Id);
                    await LoadWorkspacesAsync();
                }
            }
        }

        private async void NavigationControl_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (_isUpdatingSelection) return;

            async Task<bool> ConfirmNavigationAsync()
            {
                if (ContentFrame.Content is IUnsavedChangesGuard guard && guard.HasUnsavedChanges)
                {
                    return await guard.ConfirmLeaveAsync();
                }
                return true;
            }

            void RestorePreviousSelection()
            {
                _isUpdatingSelection = true;
                NavigationControl.SelectedItem = _lastSelectedNavigationItem;
                _isUpdatingSelection = false;
            }

            if (args.IsSettingsSelected)
            {
                if (!await ConfirmNavigationAsync())
                {
                    RestorePreviousSelection();
                    return;
                }
                _navigationService.Navigate(typeof(SettingsPage));
                _lastSelectedNavigationItem = null;
                return;
            }

            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                if (selectedItem == HomeMenuItem)
                {
                    if (!await ConfirmNavigationAsync())
                    {
                        RestorePreviousSelection();
                        return;
                    }
                    _navigationService.Navigate(typeof(HomePage));
                    _lastSelectedNavigationItem = selectedItem;
                }
                else if (selectedItem == ActionsMenuItem)
                {
                    if (!await ConfirmNavigationAsync())
                    {
                        RestorePreviousSelection();
                        return;
                    }
                    _navigationService.Navigate(typeof(ActionsPage));
                    _lastSelectedNavigationItem = selectedItem;
                }
                else if (selectedItem.Tag is Guid projectId)
                {
                    if (!await ConfirmNavigationAsync())
                    {
                        RestorePreviousSelection();
                        return;
                    }
                    _appStateService.CurrentProjectId = projectId;
                    _navigationService.Navigate(typeof(ProjectPage), projectId);
                    _lastSelectedNavigationItem = selectedItem;
                }
                else if (selectedItem.Tag?.ToString() == "ADD_PROJECT")
                {
                    // Revert selection to avoid highlighting "Add Project"
                    NavigationControl.SelectedItem = HomeMenuItem;
                    
                    if (_appStateService.CurrentWorkspaceId.HasValue)
                    {
                        var project = await _dialogService.ShowProjectEditorAsync(_appStateService.CurrentWorkspaceId.Value, null, XamlRoot);
                        if (project != null)
                        {
                            var created = await _projectService.AddProjectAsync(
                                _appStateService.CurrentWorkspaceId.Value,
                                project.Name,
                                project.FolderPath,
                                project.IconKind,
                                project.IconValue,
                                project.DefaultShell,
                                project.IsPinned);

                            await LoadProjectsAsync(_appStateService.CurrentWorkspaceId.Value);
                        }
                    }
                    else
                    {
                        await _dialogService.ShowMessageAsync("Cảnh báo", "Vui lòng tạo hoặc chọn một Workspace trước khi thêm Project.", XamlRoot);
                    }
                }
            }
        }

        public async Task RefreshProjectsListAsync()
        {
            if (_appStateService.CurrentWorkspaceId.HasValue)
            {
                await LoadProjectsAsync(_appStateService.CurrentWorkspaceId.Value);
            }
        }

        public async Task<bool> ConfirmCurrentPageLeaveAsync()
        {
            if (ContentFrame.Content is IUnsavedChangesGuard guard && guard.HasUnsavedChanges)
            {
                return await guard.ConfirmLeaveAsync();
            }

            return true;
        }

        public bool HasCurrentPageUnsavedChanges =>
            ContentFrame.Content is IUnsavedChangesGuard guard && guard.HasUnsavedChanges;

        private async void Settings_Accelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (!await ConfirmCurrentPageLeaveAsync())
            {
                args.Handled = true;
                return;
            }

            _navigationService.Navigate(typeof(SettingsPage));
            NavigationControl.SelectedItem = null;
            args.Handled = true;
        }
    }
}
