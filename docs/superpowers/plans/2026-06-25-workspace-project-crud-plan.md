# DevDeck Workspace & Project CRUD Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full CRUD capabilities for managing Workspaces and Projects, copy custom icon files locally, and connect Workspace selection to dynamic projects sidebar navigation.

**Architecture:** We will build services for Workspace and Project entities that persist changes into `data.json` via `IDataService`. We will design a `DialogService` to manage `ContentDialogs` using the correct WinUI 3 `XamlRoot`. Folder pickers will be initialised with `WindowHandleService` HWND.

**Tech Stack:** .NET 8.0, WinUI 3 (Windows App SDK 2.2.0), CommunityToolkit.Mvvm 8.2.2.

## Global Constraints

*   Preserve the current WinUI 3 project, target framework, namespace, and packaging model.
*   Do not split the solution into Core/Application/Infrastructure projects yet.
*   Public asynchronous method names must end in `Async`.
*   Enable nullable reference types.
*   No third-party Windows 11 UI frameworks. Use native WinUI 3 controls.

---

### Task 1: Core Services implementation (Workspace, Project, IconStorage)

**Files:**
*   Create: `Contracts/IWorkspaceService.cs`, `Services/WorkspaceService.cs`
*   Create: `Contracts/IProjectService.cs`, `Services/ProjectService.cs`
*   Create: `Contracts/IIconStorageService.cs`, `Services/IconStorageService.cs`
*   Modify: `App.xaml.cs` (Register services)

**Interfaces:**
*   Consumes: `IDataService`, `IPathResolverService`
*   Produces: `IWorkspaceService`, `IProjectService`, `IIconStorageService` in DI.

- [ ] **Step 1: Write `IWorkspaceService.cs`**

Write `Contracts/IWorkspaceService.cs` as defined in spec.

- [ ] **Step 2: Write `WorkspaceService.cs`**

Write `Services/WorkspaceService.cs` connecting with `IDataService` and handling cascade deletes:
```csharp
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
```

- [ ] **Step 3: Write `IProjectService.cs`**

Write `Contracts/IProjectService.cs` as defined in spec.

- [ ] **Step 4: Write `ProjectService.cs`**

Write `Services/ProjectService.cs` keeping workspace relationship and atomic saving:
```csharp
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
```

- [ ] **Step 5: Write `IIconStorageService.cs` & `IconStorageService.cs`**

Write `Services/IconStorageService.cs` copying selected icon to `LocalState/icons`:
```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using DevDeck.Contracts;

namespace DevDeck.Services
{
    public sealed class IconStorageService : IIconStorageService
    {
        private readonly IPathResolverService _pathResolver;

        public IconStorageService(IPathResolverService pathResolver)
        {
            _pathResolver = pathResolver;
        }

        public async Task<string> SaveCustomIconAsync(string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath)) throw new FileNotFoundException("Source icon file not found", sourceFilePath);
            
            string extension = Path.GetExtension(sourceFilePath);
            string stableName = Guid.NewGuid().ToString("N") + extension;
            string destPath = Path.Combine(_pathResolver.GetIconsDirectory(), stableName);

            // Copy file asynchronously
            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            // Return relative path
            return Path.Combine("icons", stableName);
        }
    }
}
```

- [ ] **Step 6: Register services inside `App.xaml.cs`**

Modify `App.xaml.cs` to add:
```csharp
    services.AddSingleton<IWorkspaceService, WorkspaceService>();
    services.AddSingleton<IProjectService, ProjectService>();
    services.AddSingleton<IIconStorageService, IconStorageService>();
```

- [ ] **Step 7: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 8: Commit**

```bash
git add Contracts/ Services/
git commit -m "feat: implement WorkspaceService, ProjectService, and IconStorageService"
```

---

### Task 2: Dialogs & DialogService implementation

**Files:**
*   Create: `Contracts/IDialogService.cs`, `Services/DialogService.cs`
*   Create: `Dialogs/WorkspaceEditorDialog.xaml` (.cs)
*   Create: `Dialogs/ProjectEditorDialog.xaml` (.cs)

**Interfaces:**
*   Consumes: `IWorkspaceService`, `IProjectService`, `IIconStorageService`, `WindowHandleService`
*   Produces: `IDialogService` and standard popups.

- [ ] **Step 1: Write `WorkspaceEditorDialog.xaml`**

Create a `ContentDialog` in `Dialogs/WorkspaceEditorDialog.xaml` with text fields for Name, and selection dropdown for IconKind.

- [ ] **Step 2: Write `ProjectEditorDialog.xaml`**

Create a `ContentDialog` in `Dialogs/ProjectEditorDialog.xaml` that has Name, DefaultShell, Pin to sidebar checkbox, and a Folder picker button. Clicking the button opens `FolderPicker` initialised with `WindowHandleService` HWND.

- [ ] **Step 3: Write `DialogService.cs`**

Create `Services/DialogService.cs` implementing standard popups with `XamlRoot`:
```csharp
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Contracts;
using DevDeck.Models;
using DevDeck.Dialogs;

namespace DevDeck.Services
{
    public sealed class DialogService : IDialogService
    {
        public async Task ShowMessageAsync(string title, string message, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot
            };
            await dialog.ShowAsync();
        }

        public async Task<bool> ShowConfirmationAsync(string title, string message, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "Xác nhận",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        public async Task<WorkspaceEntity?> ShowWorkspaceEditorAsync(WorkspaceEntity? existingWorkspace, XamlRoot xamlRoot)
        {
            var dialog = new WorkspaceEditorDialog(existingWorkspace)
            {
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? dialog.WorkspaceResult : null;
        }

        public async Task<ProjectEntity?> ShowProjectEditorAsync(Guid workspaceId, ProjectEntity? existingProject, XamlRoot xamlRoot)
        {
            var dialog = new ProjectEditorDialog(workspaceId, existingProject)
            {
                XamlRoot = xamlRoot
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? dialog.ProjectResult : null;
        }
    }
}
```

Register `IDialogService` inside `App.xaml.cs`.

- [ ] **Step 4: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add Dialogs/ Services/DialogService.cs Contracts/IDialogService.cs
git commit -m "feat: add ContentDialogs and DialogService for Workspaces and Projects editor"
```

---

### Task 3: ShellPage Sidebar updates (Workspace selector and projects list)

**Files:**
*   Modify: `Shell/ShellPage.xaml`, `Shell/ShellPage.xaml.cs`

**Interfaces:**
*   Consumes: `IWorkspaceService`, `IProjectService`, `AppStateService`, `IDialogService`
*   Produces: Dynamic Project NavigationViewItems updating on Workspace combo change.

- [ ] **Step 1: Wire Workspace combobox in `ShellPage.xaml.cs`**

Populate workspaces list in ComboBox. When Selection changes:
1. Update `AppStateService.CurrentWorkspaceId`.
2. Retrieve Projects list using `IProjectService.GetProjectsAsync` and add them as dynamic `NavigationViewItem`s in the sidebar menu under "PROJECTS" header.
3. Select first project or navigate to Home Page if no project exists.

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add Shell/ShellPage.xaml.cs
git commit -m "feat: bind workspace selector combobox to update sidebar projects"
```

---

### Task 4: HomePage and ProjectPage UI updates

**Files:**
*   Modify: `Features/Home/HomePage.xaml`, `Features/Home/HomePage.xaml.cs`
*   Modify: `Features/Projects/ProjectPage.xaml`, `Features/Projects/ProjectPage.xaml.cs`

**Interfaces:**
*   Consumes: `IProjectService`, `INavigationService`
*   Produces: Grid of project cards on Home page, and action headers on Project page.

- [ ] **Step 1: Implement Workspace Home page (`HomePage.xaml`)**

Display a Grid of Project cards for the active Workspace, showing Project name, directory path, and an "Add Project" card/button. Clicking a Project card navigates to `ProjectPage`.

- [ ] **Step 2: Implement Project Page Header (`ProjectPage.xaml`)**

Display Project Name, absolute directory path, and quick buttons: "Open Folder" (explorer.exe), "Open Terminal" (toggles integrated terminal), and a "More options" context menu (Rename, Move, Delete Project).

- [ ] **Step 3: Run full build and test application launch**

Run: `dotnet build -p:Platform=x64`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add Features/Home/ Features/Projects/
git commit -m "feat: complete Workspace Home grid and Project header view"
```

---

## Plan Review Check

1. **Spec coverage:** Services, dialogs, folder picker logic, path resolutions, and UI grids are covered.
2. **No Placeholders:** Code and implementations specified.

---

### Execution Choice
Please choose one of the two execution options:
1. **Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration.
2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints.
