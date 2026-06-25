using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevDeck.Contracts;
using DevDeck.Data;
using DevDeck.Models;

namespace DevDeck.Services
{
    public sealed class ActionService : IActionService
    {
        private readonly IDataService _dataService;

        public ActionService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public Task<IReadOnlyList<DevActionEntity>> GetActionsAsync()
        {
            var list = _dataService.Data.GlobalActions.OrderBy(a => a.SortOrder).ToList();
            return Task.FromResult<IReadOnlyList<DevActionEntity>>(list);
        }

        public async Task<DevActionEntity> CreateActionAsync(DevActionEntity action)
        {
            int maxOrder = _dataService.Data.GlobalActions.Count > 0 ? _dataService.Data.GlobalActions.Max(a => a.SortOrder) : 0;
            action.Id = Guid.NewGuid();
            action.SortOrder = maxOrder + 1;

            foreach (var step in action.Steps)
            {
                step.DevActionId = action.Id;
            }
            
            _dataService.Data.GlobalActions.Add(action);
            await _dataService.SaveAsync();
            return action;
        }

        public async Task UpdateActionAsync(DevActionEntity action)
        {
            var existing = _dataService.Data.GlobalActions.FirstOrDefault(a => a.Id == action.Id);
            if (existing != null)
            {
                existing.Name = action.Name;
                existing.IconKind = action.IconKind;
                existing.IconValue = action.IconValue;
                existing.Scope = action.Scope;
                existing.GroupName = action.GroupName;
                existing.ExecutionMode = action.ExecutionMode;
                existing.StopOnFailure = action.StopOnFailure;
                existing.RequireConfirmation = action.RequireConfirmation;
                existing.AllowConcurrentRuns = action.AllowConcurrentRuns;
                
                // Replace steps
                existing.Steps.Clear();
                foreach (var step in action.Steps)
                {
                    step.DevActionId = action.Id;
                    existing.Steps.Add(step);
                }

                await _dataService.SaveAsync();
            }
        }

        public async Task DeleteActionAsync(Guid actionId)
        {
            var existing = _dataService.Data.GlobalActions.FirstOrDefault(a => a.Id == actionId);
            if (existing != null)
            {
                _dataService.Data.GlobalActions.Remove(existing);
                
                // Clean up assignments
                foreach (var ws in _dataService.Data.Workspaces)
                {
                    foreach (var proj in ws.Projects)
                    {
                        var pa = proj.ProjectActions.FirstOrDefault(a => a.DevActionId == actionId);
                        if (pa != null)
                        {
                            proj.ProjectActions.Remove(pa);
                        }
                    }
                }

                await _dataService.SaveAsync();
            }
        }

        public async Task AssignActionToProjectAsync(Guid actionId, Guid projectId)
        {
            foreach (var ws in _dataService.Data.Workspaces)
            {
                var proj = ws.Projects.FirstOrDefault(p => p.Id == projectId);
                if (proj != null)
                {
                    if (!proj.ProjectActions.Any(a => a.DevActionId == actionId))
                    {
                        var action = _dataService.Data.GlobalActions.FirstOrDefault(a => a.Id == actionId);
                        if (action != null)
                        {
                            int maxOrder = proj.ProjectActions.Count > 0 ? proj.ProjectActions.Max(a => a.DisplayOrder) : 0;
                            proj.ProjectActions.Add(new ProjectActionEntity
                            {
                                ProjectId = projectId,
                                DevActionId = actionId,
                                DisplayOrder = maxOrder + 1,
                                Project = proj,
                                DevAction = action
                            });
                            await _dataService.SaveAsync();
                        }
                    }
                    break;
                }
            }
        }

        public async Task RemoveActionFromProjectAsync(Guid actionId, Guid projectId)
        {
            foreach (var ws in _dataService.Data.Workspaces)
            {
                var proj = ws.Projects.FirstOrDefault(p => p.Id == projectId);
                if (proj != null)
                {
                    var pa = proj.ProjectActions.FirstOrDefault(a => a.DevActionId == actionId);
                    if (pa != null)
                    {
                        proj.ProjectActions.Remove(pa);
                        await _dataService.SaveAsync();
                    }
                    break;
                }
            }
        }

        public Task<IReadOnlyList<ProjectActionEntity>> GetProjectActionsAsync(Guid projectId)
        {
            foreach (var ws in _dataService.Data.Workspaces)
            {
                var proj = ws.Projects.FirstOrDefault(p => p.Id == projectId);
                if (proj != null)
                {
                    var list = proj.ProjectActions.OrderBy(a => a.DisplayOrder).ToList();
                    return Task.FromResult<IReadOnlyList<ProjectActionEntity>>(list);
                }
            }
            return Task.FromResult<IReadOnlyList<ProjectActionEntity>>([]);
        }

        public async Task UpdateProjectActionAsync(ProjectActionEntity projectAction)
        {
            foreach (var ws in _dataService.Data.Workspaces)
            {
                var proj = ws.Projects.FirstOrDefault(p => p.Id == projectAction.ProjectId);
                if (proj != null)
                {
                    var existing = proj.ProjectActions.FirstOrDefault(pa => pa.DevActionId == projectAction.DevActionId);
                    if (existing != null)
                    {
                        existing.NameOverride = string.IsNullOrEmpty(projectAction.NameOverride?.Trim()) ? null : projectAction.NameOverride.Trim();
                        existing.IconKindOverride = projectAction.IconKindOverride;
                        existing.IconValueOverride = projectAction.IconValueOverride;
                        existing.GroupNameOverride = string.IsNullOrEmpty(projectAction.GroupNameOverride?.Trim()) ? null : projectAction.GroupNameOverride.Trim();
                        existing.RequireConfirmationOverride = projectAction.RequireConfirmationOverride;
                        existing.StopOnFailureOverride = projectAction.StopOnFailureOverride;
                        existing.AllowConcurrentRunsOverride = projectAction.AllowConcurrentRunsOverride;

                        // Update Step Overrides
                        existing.StepOverrides.Clear();
                        if (projectAction.StepOverrides != null)
                        {
                            foreach (var stepOver in projectAction.StepOverrides)
                            {
                                existing.StepOverrides.Add(stepOver);
                            }
                        }

                        await _dataService.SaveAsync();
                    }
                    break;
                }
            }
        }
    }
}
