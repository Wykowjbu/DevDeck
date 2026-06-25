using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevDeck.Models;
using DevDeck.Enums;

namespace DevDeck.Contracts
{
    public interface IWorkspaceService
    {
        Task<IReadOnlyList<WorkspaceEntity>> GetWorkspacesAsync();
        Task<WorkspaceEntity> CreateWorkspaceAsync(string name, IconKind iconKind, string? iconValue, string? accentColor);
        Task UpdateWorkspaceAsync(WorkspaceEntity workspace);
        Task DeleteWorkspaceAsync(Guid workspaceId);
    }
}
