using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using DevDeck.Models;

namespace DevDeck.Contracts
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message, XamlRoot xamlRoot);
        Task<bool> ShowConfirmationAsync(string title, string message, XamlRoot xamlRoot);
        Task<WorkspaceEntity?> ShowWorkspaceEditorAsync(WorkspaceEntity? existingWorkspace, XamlRoot xamlRoot);
        Task<ProjectEntity?> ShowProjectEditorAsync(Guid workspaceId, ProjectEntity? existingProject, XamlRoot xamlRoot);
        Task<ProjectActionEntity?> ShowProjectActionOverrideAsync(ProjectActionEntity existing, XamlRoot xamlRoot);
    }
}
