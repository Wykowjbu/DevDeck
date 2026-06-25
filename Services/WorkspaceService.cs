using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevDeck.Contracts;
using DevDeck.Data;
using DevDeck.Enums;
using DevDeck.Models;

namespace DevDeck.Services
{
    public sealed class WorkspaceService : IWorkspaceService
    {
        private readonly IDataService _dataService;

        public WorkspaceService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public Task<IReadOnlyList<WorkspaceEntity>> GetWorkspacesAsync()
        {
            var list = _dataService.Data.Workspaces.OrderBy(w => w.SortOrder).ToList();
            return Task.FromResult<IReadOnlyList<WorkspaceEntity>>(list);
        }

        public async Task<WorkspaceEntity> CreateWorkspaceAsync(string name, IconKind iconKind, string? iconValue, string? accentColor)
        {
            int maxOrder = _dataService.Data.Workspaces.Count > 0 ? _dataService.Data.Workspaces.Max(w => w.SortOrder) : 0;
            var workspace = new WorkspaceEntity
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                IconKind = iconKind,
                IconValue = iconValue,
                AccentColor = accentColor,
                SortOrder = maxOrder + 1
            };

            _dataService.Data.Workspaces.Add(workspace);
            await _dataService.SaveAsync();
            return workspace;
        }

        public async Task UpdateWorkspaceAsync(WorkspaceEntity workspace)
        {
            var existing = _dataService.Data.Workspaces.FirstOrDefault(w => w.Id == workspace.Id);
            if (existing != null)
            {
                existing.Name = workspace.Name.Trim();
                existing.IconKind = workspace.IconKind;
                existing.IconValue = workspace.IconValue;
                existing.AccentColor = workspace.AccentColor;
                existing.SortOrder = workspace.SortOrder;
                await _dataService.SaveAsync();
            }
        }

        public async Task DeleteWorkspaceAsync(Guid workspaceId)
        {
            var existing = _dataService.Data.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
            if (existing != null)
            {
                // Cascade delete Projects
                existing.Projects.Clear();
                
                // Cascade delete actions owned by this workspace
                _dataService.Data.GlobalActions.RemoveAll(a => a.OwnerWorkspaceId == workspaceId);

                _dataService.Data.Workspaces.Remove(existing);
                await _dataService.SaveAsync();
            }
        }
    }
}
