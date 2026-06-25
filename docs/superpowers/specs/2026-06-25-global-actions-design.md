# DevDeck Global Actions Design Spec

> **Date:** 2026-06-25  
> **Target:** Phase 5 (Global Action Library)  
> **Status:** Draft  

This specification defines the components, interfaces, view models, and editor pages required to implement the Global Action library and Project page integration in DevDeck.

---

## 1. Directory Structure Additions

```text
DevDeck/
├── Services/
│   └── ActionService.cs
│
├── Contracts/
│   └── IActionService.cs
│
├── Controls/
│   └── ActionButtonControl.xaml (.cs)
│
└── Dialogs/
    ├── ActionEditorDialog.xaml (.cs)
    └── ActionAssignmentDialog.xaml (.cs)
```

---

## 2. Action Service Interface

Handles CRUD operations on actions and steps, and manages project assignments.

`Contracts/IActionService.cs`:
```csharp
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
    }
}
```

`Services/ActionService.cs` directly accesses `IDataService.Data.GlobalActions` and the workspace/project collection to manage assignments (mapping instances of `ProjectActionEntity` in `ProjectEntity.ProjectActions`).

---

## 3. UI Component Design

### 3.1. ActionsPage Layout
Two-pane split grid:
*   **Left Pane:** Search box, "New Action" button, ListView of actions.
*   **Right Pane:** Selected action details, including visual list of steps, Scope label, Assigned Projects tags list, and Action editing trigger buttons.

### 3.2. Action Editor Dialog (`ActionEditorDialog.xaml`)
Allows configuring:
*   Name, Group name, Scope, ExecutionMode, StopOnFailure, RequireConfirmation, AllowConcurrentRuns.
*   **Steps collection editor:** An inline list where users can Add, Remove, and Edit steps.
    *   Dropdown for `StepType`. Fields change based on selected step type (e.g. Command text for ShellCommand; delay millisecond value for Delay step).

### 3.3. ActionButtonControl (`Controls/ActionButtonControl.xaml`)
A reusable WinUI 3 button designed specifically for Actions:
*   Dimensions: Height = 42px, CornerRadius = 6px, SemiBold 14px font.
*   Left Click: Executes the action (wiring in Phase 7).
*   Right Click: Exposes a `MenuFlyout` context menu (Customize override, Run, Remove).
*   States (Visual representation):
    *   Idle: Shows action icon + name.
    *   Running: Shows `ProgressRing` + name.
    *   Succeeded: Shows briefly a green Check symbol.
    *   Failed: Shows briefly a red Warning symbol.

---

## 4. UI Shell & Project Page Integration

On `ProjectPage`:
*   Actions are retrieved via `IActionService.GetProjectActionsAsync(projectId)`.
*   Grouped by `GroupName` (defaults to "OTHER" if null).
*   Presented under Headers on the "Actions" PivotItem using `ItemsControl` or `WrapPanel`.

---
