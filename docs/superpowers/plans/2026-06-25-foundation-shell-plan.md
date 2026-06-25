# DevDeck Foundation & Shell Implementation Plan (JSON Storage Version)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Setup DI, JSON data storage, JSON settings, and the Windows 11 Navigation Shell with a custom title bar and lazy terminal panel placeholder for DevDeck.

**Architecture:** We will set up a Hosting environment (`IHost`) in `App.xaml.cs` to manage services. The data layer will use file-based JSON storage (`data.json`) managed by a singleton `DataService` with automated loading and atomic saving on modifications.

**Tech Stack:** .NET 8.0, WinUI 3 (Windows App SDK 2.2.0), CommunityToolkit.Mvvm 8.2.2, Microsoft.Extensions.Hosting 8.0.0.

## Global Constraints

*   Preserve the current WinUI 3 project, target framework (`net8.0-windows10.0.19041.0`), namespace (`DevDeck`), and packaging model.
*   Do not split the solution into Core/Application/Infrastructure projects yet.
*   Public asynchronous method names must end in `Async`.
*   Enable nullable reference types.
*   No third-party Windows 11 UI frameworks. Use native WinUI 3 controls.

---

### Task 1: NuGet Package Setup & Directory Structure

**Files:**
*   Modify: `DevDeck.csproj`
*   Create directories: `Shell`, `Features`, `Controls`, `Models`, `Enums`, `Data`, `Services`, `Contracts`, `Themes`, `Helpers`, `Features/Home`, `Features/Projects`, `Features/Actions`, `Features/Settings`

**Interfaces:**
*   Consumes: None
*   Produces: Baseline project with required libraries and folder structure.

- [ ] **Step 1: Update NuGet Package References in `DevDeck.csproj`**

Update `DevDeck.csproj` to include Hosting, Logging, Sizers, and MVVM toolkit packages:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.28000.1839" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="2.2.0" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
    <PackageReference Include="CommunityToolkit.WinUI.Controls.Sizers" Version="8.0.240109" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.0.0" />
  </ItemGroup>
```

- [ ] **Step 2: Run build to restore packages**

Run: `dotnet build` in `D:\Users\huynp29052004\Projects\DevDeck\DevDeck`
Expected: Build succeeds with 0 errors.

- [ ] **Step 3: Create project subfolders**

Create empty directories inside `D:\Users\huynp29052004\Projects\DevDeck\DevDeck`:
*   `Shell`, `Features`, `Features/Home`, `Features/Projects`, `Features/Actions`, `Features/Settings`, `Controls`, `Models`, `Enums`, `Data`, `Services`, `Contracts`, `Themes`, `Helpers`

- [ ] **Step 4: Commit**

```bash
git add DevDeck.csproj
git commit -m "chore: setup nuget packages and create project directories"
```

---

### Task 2: Platform Foundation Services & DI Host Setup

**Files:**
*   Create: `Contracts/IPathResolverService.cs`, `Services/PathResolverService.cs`
*   Create: `Services/WindowHandleService.cs`, `Services/AppStateService.cs`
*   Modify: `App.xaml.cs`

**Interfaces:**
*   Consumes: Microsoft.Extensions.Hosting, Microsoft.Extensions.DependencyInjection
*   Produces: `IPathResolverService`, `WindowHandleService`, `AppStateService` registered in `App.AppHost`.

*(Steps remain identical to original plan)*

---

### Task 3: JSON Data Entities and DataService Setup

**Files:**
*   Create: `Enums/*.cs`
*   Create: `Models/WorkspaceEntity.cs`, `Models/ProjectEntity.cs`, `Models/DevActionEntity.cs`, `Models/ActionStepEntity.cs`, `Models/ProjectActionEntity.cs`, `Models/ProjectActionStepOverrideEntity.cs`
*   Create: `Models/DevDeckData.cs`
*   Create: `Data/IDataService.cs`, `Data/DataService.cs`
*   Modify: `App.xaml.cs` (Register DataService & load data on startup)

**Interfaces:**
*   Consumes: `IPathResolverService`
*   Produces: `IDataService` registered in DI.

- [ ] **Step 1: Create Enums**

*(Create Enums identical to original plan)*

- [ ] **Step 2: Create Data Entities**

*(Create Models identical to original plan)*

- [ ] **Step 3: Create `DevDeckData.cs`**

Write `Models/DevDeckData.cs`:
```csharp
using System.Collections.Generic;

namespace DevDeck.Models
{
    public sealed class DevDeckData
    {
        public List<WorkspaceEntity> Workspaces { get; set; } = [];
        public List<DevActionEntity> GlobalActions { get; set; } = [];
    }
}
```

- [ ] **Step 4: Create `IDataService.cs`**

Write `Data/IDataService.cs`:
```csharp
using System.Threading.Tasks;
using DevDeck.Models;

namespace DevDeck.Data
{
    public interface IDataService
    {
        DevDeckData Data { get; }
        Task LoadAsync();
        Task SaveAsync();
    }
}
```

- [ ] **Step 5: Create `DataService.cs`**

Write `Data/DataService.cs` using atomic file replacement and exception handling:
```csharp
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevDeck.Contracts;
using DevDeck.Models;

namespace DevDeck.Data
{
    public sealed class DataService : IDataService
    {
        private readonly IPathResolverService _pathResolver;
        private readonly ILogger<DataService> _logger;
        private DevDeckData _data = new();

        public DevDeckData Data => _data;

        public DataService(IPathResolverService pathResolver, ILogger<DataService> logger)
        {
            _pathResolver = pathResolver;
            _logger = logger;
        }

        public async Task LoadAsync()
        {
            string path = Path.Combine(_pathResolver.GetAppDataDirectory(), "data.json");
            if (!File.Exists(path))
            {
                _data = new DevDeckData();
                return;
            }

            try
            {
                string json = await File.ReadAllTextAsync(path);
                _data = JsonSerializer.Deserialize<DevDeckData>(json) ?? new DevDeckData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load data.json. Initializing empty data.");
                _data = new DevDeckData();
            }
        }

        public async Task SaveAsync()
        {
            string path = Path.Combine(_pathResolver.GetAppDataDirectory(), "data.json");
            string tempPath = path + ".tmp";

            try
            {
                string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(tempPath, json);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save data.json atomically.");
            }
        }
    }
}
```

- [ ] **Step 6: Register DataService in AppHost**

Modify `App.xaml.cs` to register `IDataService` and call `LoadAsync()` on startup:
```csharp
    services.AddSingleton<IDataService, DataService>();
```
In `OnLaunched()`:
```csharp
    var dataService = AppHost.Services.GetRequiredService<IDataService>();
    await dataService.LoadAsync();
```

- [ ] **Step 7: Build project to check compilation**

Run: `dotnet build`
Expected: Build succeeds with 0 errors.

- [ ] **Step 8: Commit**

```bash
git add Models/DevDeckData.cs Data/IDataService.cs Data/DataService.cs
git commit -m "feat: add JSON data storage models and DataService"
```

---

### Task 4: JSON Settings Service Setup

*(Remains identical to original plan)*

---

### Task 5: Custom Title Bar & MainWindow Theme Setup

*(Remains identical to original plan)*

---

### Task 6: Navigation Service, Shell Layout and Placeholders Setup

*(Remains identical to original plan)*

---
