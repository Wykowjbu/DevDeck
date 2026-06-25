using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Contracts;
using DevDeck.Models;
using DevDeck.Dialogs;

namespace DevDeck.Services
{
    public sealed class DialogService : IDialogService
    {
        public async Task ShowMessageAsync(string title, string message, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await dialog.ShowAsync();
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Xác nhận",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        public async Task<WorkspaceEntity?> ShowWorkspaceEditorAsync(WorkspaceEntity? existingWorkspace, XamlRoot xamlRoot)
        {
            var dialog = new WorkspaceEditorDialog(existingWorkspace)
            {
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? dialog.WorkspaceResult : null;
        }

        public async Task<ProjectEntity?> ShowProjectEditorAsync(Guid workspaceId, ProjectEntity? existingProject, XamlRoot xamlRoot)
        {
            var dialog = new ProjectEditorDialog(workspaceId, existingProject)
            {
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? dialog.ProjectResult : null;
        }

        public async Task<ProjectActionEntity?> ShowProjectActionOverrideAsync(ProjectActionEntity existing, XamlRoot xamlRoot)
        {
            var dialog = new ProjectActionOverrideDialog(existing)
            {
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? dialog.Result : null;
        }
    }
}
