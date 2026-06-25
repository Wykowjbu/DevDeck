using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevDeck.Contracts;
using DevDeck.Data;
using DevDeck.Enums;
using DevDeck.Models;

namespace DevDeck.Services
{
    public sealed class ProjectService : IProjectService
    {
        private readonly IDataService _dataService;

        public ProjectService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public Task<IReadOnlyList<ProjectEntity>> GetProjectsAsync(Guid workspaceId)
        {
            var workspace = _dataService.Data.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (workspace == null)
            {
                return Task.FromResult<IReadOnlyList<ProjectEntity>>([]);
            }
            var list = workspace.Projects.OrderBy(p => p.SortOrder).ToList();
            return Task.FromResult<IReadOnlyList<ProjectEntity>>(list);
        }

        public async Task<ProjectEntity> AddProjectAsync(Guid workspaceId, string name, string folderPath, IconKind iconKind, string? iconValue, ShellType defaultShell, bool isPinned)
        {
            var workspace = _dataService.Data.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (workspace == null) throw new InvalidOperationException("Workspace not found");

            int maxOrder = workspace.Projects.Count > 0 ? workspace.Projects.Max(p => p.SortOrder) : 0;
            var project = new ProjectEntity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                Name = name.Trim(),
                FolderPath = folderPath.Trim(),
                IconKind = iconKind,
                IconValue = iconValue,
                DefaultShell = defaultShell,
                IsPinned = isPinned,
                SortOrder = maxOrder + 1,
                Workspace = workspace
            };

            workspace.Projects.Add(project);
            await _dataService.SaveAsync();
            return project;
        }

        public async Task UpdateProjectAsync(ProjectEntity project)
        {
            var workspace = _dataService.Data.Workspaces.FirstOrDefault(w => w.Id == project.WorkspaceId);
            if (workspace != null)
            {
                var existing = workspace.Projects.FirstOrDefault(p => p.Id == project.Id);
                if (existing != null)
                {
                    existing.Name = project.Name.Trim();
                    existing.FolderPath = project.FolderPath.Trim();
                    existing.IconKind = project.IconKind;
                    existing.IconValue = project.IconValue;
                    existing.DefaultShell = project.DefaultShell;
                    existing.IsPinned = project.IsPinned;
                    existing.SortOrder = project.SortOrder;
                    
                    // If workspace was changed, move project
                    if (existing.WorkspaceId != project.WorkspaceId)
                    {
                        workspace.Projects.Remove(existing);
                        var newWorkspace = _dataService.Data.Workspaces.FirstOrDefault(w => w.Id == project.WorkspaceId);
                        if (newWorkspace != null)
                        {
                            existing.WorkspaceId = project.WorkspaceId;
                            existing.Workspace = newWorkspace;
                            newWorkspace.Projects.Add(existing);
                        }
                    }

                    await _dataService.SaveAsync();
                }
            }
        }

        public async Task DeleteProjectAsync(Guid projectId)
        {
            foreach (var workspace in _dataService.Data.Workspaces)
            {
                var existing = workspace.Projects.FirstOrDefault(p => p.Id == projectId);
                if (existing != null)
                {
                    workspace.Projects.Remove(existing);
                    
                    // Cascade delete actions owned by this project
                    _dataService.Data.GlobalActions.RemoveAll(a => a.OwnerProjectId == projectId);

                    await _dataService.SaveAsync();
                    break;
                }
            }
        }
    }
}
