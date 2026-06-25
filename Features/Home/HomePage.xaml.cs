using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using DevDeck.Contracts;
using DevDeck.Services;
using DevDeck.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DevDeck.Features.Home
{
    public sealed partial class HomePage : Page
    {
        private readonly AppStateService _appStateService;
        private readonly IWorkspaceService _workspaceService;
        private readonly IProjectService _projectService;
        private readonly IDialogService _dialogService;
        private readonly INavigationService _navigationService;

        public HomePage()
        {
            InitializeComponent();
            _appStateService = App.AppHost.Services.GetRequiredService<AppStateService>();
            _workspaceService = App.AppHost.Services.GetRequiredService<IWorkspaceService>();
            _projectService = App.AppHost.Services.GetRequiredService<IProjectService>();
            _dialogService = App.AppHost.Services.GetRequiredService<IDialogService>();
            _navigationService = App.AppHost.Services.GetRequiredService<INavigationService>();

            Loaded += HomePage_Loaded;
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_appStateService.CurrentWorkspaceId.HasValue)
            {
                var workspaces = await _workspaceService.GetWorkspacesAsync();
                var currentWorkspace = workspaces.FirstOrDefault(w => w.Id == _appStateService.CurrentWorkspaceId.Value);
                if (currentWorkspace != null)
                {
                    WorkspaceTitle.Text = $"Workspace: {currentWorkspace.Name}";
                }

                var projects = await _projectService.GetProjectsAsync(_appStateService.CurrentWorkspaceId.Value);
                ProjectsGridView.ItemsSource = projects;
                
                AddProjectBtn.IsEnabled = true;
            }
            else
            {
                WorkspaceTitle.Text = "Không có Workspace nào";
                ProjectsGridView.ItemsSource = null;
                AddProjectBtn.IsEnabled = false;
            }
        }

        private void ProjectsGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProjectEntity project)
            {
                _appStateService.CurrentProjectId = project.Id;
                _navigationService.Navigate(typeof(Projects.ProjectPage), project.Id);
            }
        }

        private async void AddProjectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_appStateService.CurrentWorkspaceId.HasValue) return;

            var project = await _dialogService.ShowProjectEditorAsync(_appStateService.CurrentWorkspaceId.Value, null, XamlRoot);
            if (project != null)
            {
                await _projectService.AddProjectAsync(
                    _appStateService.CurrentWorkspaceId.Value,
                    project.Name,
                    project.FolderPath,
                    project.IconKind,
                    project.IconValue,
                    project.DefaultShell,
                    project.IsPinned);

                await LoadDataAsync();

                // Refresh sidebar projects in ShellPage
                if (App.MainWindow.ShellPage is Shell.ShellPage shellPage)
                {
                    await shellPage.RefreshProjectsListAsync();
                }
            }
        }
    }
}
