# DevDeck Global Actions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Action library CRUD, assignment rules, group bindings, custom button rendering, and actions page interface for DevDeck.

**Architecture:** We will implement an `ActionService` to manage `DevActionEntity` lists inside `data.json`. We will build `ActionButtonControl` to represent action states visually and `ActionsPage` for global actions management.

**Tech Stack:** .NET 8.0, WinUI 3, CommunityToolkit.Mvvm 8.2.2.

## Global Constraints

*   Preserve the current WinUI 3 project, target framework, namespace, and packaging model.
*   Do not split the solution into Core/Application/Infrastructure projects yet.
*   Public asynchronous method names must end in `Async`.
*   Enable nullable reference types.
*   No third-party Windows 11 UI frameworks. Use native WinUI 3 controls.

---

### Task 1: ActionService Implementation

**Files:**
*   Create: `Contracts/IActionService.cs`, `Services/ActionService.cs`
*   Modify: `App.xaml.cs` (Register in DI)

**Interfaces:**
*   Consumes: `IDataService`
*   Produces: `IActionService` registered in DI.

- [ ] **Step 1: Write `IActionService.cs`**

Write `Contracts/IActionService.cs` defining actions CRUD and project assignments.

- [ ] **Step 2: Write `ActionService.cs`**

Write `Services/ActionService.cs` interacting with `IDataService`:
```csharp
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
    }
}
```

Register `IActionService` inside `App.xaml.cs`.

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

---

### Task 2: ActionEditorDialog implementation

**Files:**
*   Create: `Dialogs/ActionEditorDialog.xaml` (.cs)

- [ ] **Step 1: Create `ActionEditorDialog.xaml`**

Create a `ContentDialog` layout allowing user to input Action Name, GroupName, Scope dropdown, ExecutionMode (Sequential/Parallel), Checkboxes for Confirm and Concurrency, and an expandable UI section to add/remove ActionSteps.

- [ ] **Step 2: Create code-behind `ActionEditorDialog.xaml.cs`**

Bind step values and validate input. Each Action must have a Name and at least 1 Step defined.

---

### Task 3: ActionButtonControl implementation

**Files:**
*   Create: `Controls/ActionButtonControl.xaml` (.cs)

- [ ] **Step 1: Design XAML `ActionButtonControl.xaml`**

Implement custom layout styled as button `Height="42"`, with default icon (FontIcon Document) and TextBlock for ProjectAction Name. Add a visual `ProgressRing` overlaying the icon area.

- [ ] **Step 2: Implement code-behind `ActionButtonControl.xaml.cs`**

Define `ProjectAction` DependencyProperty. Listen to Left-click (triggers action run) and Right-click (custom context menu containing customization flyout options).

---

### Task 4: ActionsPage Interface

**Files:**
*   Modify: `Features/Actions/ActionsPage.xaml` (.cs)

- [ ] **Step 1: Design `ActionsPage.xaml` Layout**

Create split columns view:
*   Left pane lists all Global Actions, with a Search Box and "+ New Action" button.
*   Right pane displays Selected Action properties, a dynamic list of Steps, and an "Assign to Projects" selection button list.

- [ ] **Step 2: Implement code-behind `ActionsPage.xaml.cs`**

Handle listView selected index changes, show dialogs for edits/deletes, and update project bindings using `IActionService`.

---

### Task 5: ProjectPage Integration

**Files:**
*   Modify: `Features/Projects/ProjectPage.xaml` (.cs)

- [ ] **Step 1: Update `ProjectPage.xaml` to display actions**

Within the "Actions" Pivot tab, add an `ItemsControl` that groups assigned actions by GroupName. Represent each action with `ActionButtonControl`.

- [ ] **Step 2: Wire bindings in `ProjectPage.xaml.cs`**

Fetch project actions on navigating and populate grouped collections.

- [ ] **Step 3: Run full build and test**

Run: `dotnet build -p:Platform=x64`
Expected: Build succeeds.

---

## Plan Review Check

1. **Spec coverage:** ActionService logic, ActionsPage UI columns, ActionEditor Dialog, and ActionButtonControl styling are integrated.
2. **No Placeholders:** All codes and paths specified.

---

### Execution Choice
Please choose one of the two execution options:
1. **Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints.
