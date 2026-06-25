# DevDeck Workspace & Project CRUD Design Spec

> **Date:** 2026-06-25  
> **Target:** Phase 4 (Workspace and Project CRUD)  
> **Status:** Draft  

This specification defines the services, dialogs, and views required to implement Workspace and Project management (CRUD) in DevDeck.

---

## 1. Directory Structure Additions

```text
DevDeck/
├── Services/
│   ├── WorkspaceService.cs
│   ├── ProjectService.cs
│   ├── IconStorageService.cs
│   └── DialogService.cs
│
├── Contracts/
│   ├── IWorkspaceService.cs
│   ├── IProjectService.cs
│   ├── IIconStorageService.cs
│   └── IDialogService.cs
│
└── Dialogs/
    ├── WorkspaceEditorDialog.xaml
    ├── WorkspaceEditorDialog.xaml.cs
    ├── ProjectEditorDialog.xaml
    └── ProjectEditorDialog.xaml.cs
```

---

## 2. Service Contracts & Implementations

### 2.1. Workspace Service
Handles creating, updating, and deleting workspaces in memory, then saving via `IDataService`.

`Contracts/IWorkspaceService.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevDeck.Models;

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
```

`Services/WorkspaceService.cs` implementation accesses `IDataService.Data.Workspaces` and calls `IDataService.SaveAsync()`. When deleting a workspace:
*   Cascade delete its Projects.
*   Cascade delete its Workspace-scoped actions (where `OwnerWorkspaceId == workspaceId`).

### 2.2. Project Service
Handles adding, editing, moving, and removing projects.

`Contracts/IProjectService.cs`:
```csharp
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
```

When deleting a project, we do NOT delete the physical folder. We only remove the Project record and its assigned actions/overrides from `data.json`.

### 2.3. Icon Storage Service
Copies custom icons (PNG, SVG, ICO) to `LocalState/icons` using a stable GUID filename, saving the relative path.

`Contracts/IIconStorageService.cs`:
```csharp
using System.Threading.Tasks;

namespace DevDeck.Contracts
{
    public interface IIconStorageService
    {
        Task<string> SaveCustomIconAsync(string sourceFilePath);
    }
}
```

---

## 3. UI Dialogs & DialogService

In WinUI 3 Desktop, any `ContentDialog` must be attached to the correct `XamlRoot` to prevent a runtime exception.

`Contracts/IDialogService.cs`:
```csharp
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace DevDeck.Contracts
{
    public interface IDialogService
    {
        Task ShowMessageAsync(string title, string message, XamlRoot xamlRoot);
        Task<bool> ShowConfirmationAsync(string title, string message, XamlRoot xamlRoot);
        Task<WorkspaceEntity?> ShowWorkspaceEditorAsync(WorkspaceEntity? existingWorkspace, XamlRoot xamlRoot);
        Task<ProjectEntity?> ShowProjectEditorAsync(Guid workspaceId, ProjectEntity? existingProject, XamlRoot xamlRoot);
    }
}
```

### 3.1. Folder Picker (Windows 11 Integration)
For the Folder Picker to show, we must initialize it with the main window's HWND handle:
```csharp
var folderPicker = new Windows.Storage.Pickers.FolderPicker();
folderPicker.FileTypeFilter.Add("*");
var hwnd = _windowHandleService.WindowHandle;
WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
var folder = await folderPicker.PickSingleFolderAsync();
```

---

## 4. UI Shell Integration

### 4.1. Workspace Selector (ComboBox)
Populated with the list of Workspaces. Switching a Workspace updates the dynamic Projects menu items in the NavigationView and triggers navigation to `HomePage` showing Project cards for that Workspace.

### 4.2. Workspace Home Page
Displays project cards, folder paths, and action counts. Clicking a project card navigates to `ProjectPage`. Includes an "Add Project" button which triggers the Folder Picker -> Project Editor workflow.

### 4.3. Project Page Header
Shows:
*   Project icon, project name, path.
*   Action buttons: "Open Folder" (uses `Process.Start("explorer.exe", folderPath)`), "Open Terminal" (toggles integrated terminal and starts shell in project path), "More" context menu (Rename, Change Icon, Move to Workspace, Remove Project).

---
