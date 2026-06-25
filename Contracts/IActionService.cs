using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevDeck.Models;

namespace DevDeck.Contracts
{
    public interface IActionService
    {
        Task<IReadOnlyList<DevActionEntity>> GetActionsAsync();
        Task<DevActionEntity> CreateActionAsync(DevActionEntity action);
        Task UpdateActionAsync(DevActionEntity action);
        Task DeleteActionAsync(Guid actionId);
        
        Task AssignActionToProjectAsync(Guid actionId, Guid projectId);
        Task RemoveActionFromProjectAsync(Guid actionId, Guid projectId);
        Task<IReadOnlyList<ProjectActionEntity>> GetProjectActionsAsync(Guid projectId);
        Task UpdateProjectActionAsync(ProjectActionEntity projectAction);
    }
}
