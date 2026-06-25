using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevDeck.Models;
using DevDeck.Enums;

namespace DevDeck.Contracts
{
    public interface IProjectService
    {
        Task<IReadOnlyList<ProjectEntity>> GetProjectsAsync(Guid workspaceId);
        Task<ProjectEntity> AddProjectAsync(Guid workspaceId, string name, string folderPath, IconKind iconKind, string? iconValue, ShellType defaultShell, bool isPinned);
        Task UpdateProjectAsync(ProjectEntity project);
        Task DeleteProjectAsync(Guid projectId);
    }
}
