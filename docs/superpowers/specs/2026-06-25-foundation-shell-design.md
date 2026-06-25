# DevDeck Foundation & Shell Design Spec (JSON Storage Version)

> **Date:** 2026-06-25  
> **Target:** Phase 1 (Foundation), Phase 2 (JSON Database & Settings), Phase 3 (Windows 11 Shell)  
> **Status:** Approved  

This specification defines the structural foundation, JSON data storage layer, settings management, and the main application shell for the DevDeck WinUI 3 project.

---

## 1. Directory Structure

The project directory will be organized inside the existing WinUI 3 project as follows:

```text
DevDeck/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
│
├── Shell/
│   ├── ShellPage.xaml
│   ├── ShellPage.xaml.cs
│   └── ShellViewModel.cs
│
├── Features/
│   ├── Home/
│   │   ├── HomePage.xaml
│   │   └── HomeViewModel.cs
│   │
│   ├── Projects/
│   │   ├── ProjectPage.xaml
│   │   └── ProjectViewModel.cs
│   │
│   ├── Actions/
│   │   ├── ActionsPage.xaml
│   │   └── ActionsViewModel.cs
│   │
│   └── Settings/
│       ├── SettingsPage.xaml
│       └── SettingsViewModel.cs
│
├── Controls/
│   ├── TerminalPanelControl.xaml
│   └── TerminalPanelControl.xaml.cs
│
├── Models/
│   ├── WorkspaceEntity.cs
│   ├── ProjectEntity.cs
│   ├── DevActionEntity.cs
│   ├── ActionStepEntity.cs
│   ├── ProjectActionEntity.cs
│   ├── ProjectActionStepOverrideEntity.cs
│   ├── EffectiveProjectAction.cs
│   ├── TerminalSession.cs
│   ├── AppSettings.cs
│   └── DevDeckData.cs
│
├── Enums/
│   ├── ActionScope.cs
│   ├── ActionStepType.cs
│   ├── ActionExecutionMode.cs
│   ├── ActionOutputMode.cs
│   ├── ShellType.cs
│   └── RunState.cs
│   └── IconKind.cs
│   └── AppTheme.cs
│   └── BackdropKind.cs
│   └── ActionButtonSize.cs
│
├── Data/
│   ├── IDataService.cs
│   └── DataService.cs
│
├── Services/
│   ├── NavigationService.cs
│   ├── AppStateService.cs
│   ├── SettingsService.cs
│   ├── WindowHandleService.cs
│   └── PathResolverService.cs
│
└── Contracts/
    ├── INavigationService.cs
    ├── ISettingsService.cs
    └── IPathResolverService.cs
```

---

## 2. Phase 1: Application Foundation

### 2.1. Host Configuration (`App.xaml.cs`)
We will use `Microsoft.Extensions.Hosting` to configure the DI container. The host will manage logging, configuration, DataService, and services.

```csharp
public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;
    public static Window MainWindow { get; private set; } = null!;

    public App()
    {
        InitializeComponent();

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<IPathResolverService, PathResolverService>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IDataService, DataService>();
                services.AddSingleton<AppStateService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<WindowHandleService>();

                // ViewModels
                services.AddTransient<ShellViewModel>();
                services.AddTransient<HomeViewModel>();
                services.AddTransient<ProjectViewModel>();
                services.AddTransient<ActionsViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
                services.AddTransient<ShellPage>();
                services.AddTransient<HomePage>();
                services.AddTransient<ProjectPage>();
                services.AddTransient<ActionsPage>();
                services.AddTransient<SettingsPage>();
            })
            .Build();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await AppHost.StartAsync();

        // Load Settings
        var settingsService = AppHost.Services.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        // Load Data
        var dataService = AppHost.Services.GetRequiredService<IDataService>();
        await dataService.LoadAsync();

        MainWindow = AppHost.Services.GetRequiredService<MainWindow>();
        
        // Register window handle
        var handleService = AppHost.Services.GetRequiredService<WindowHandleService>();
        handleService.RegisterWindow(MainWindow);

        // Navigate MainWindow to ShellPage
        var shellPage = AppHost.Services.GetRequiredService<ShellPage>();
        MainWindow.NavigateToShellPage(shellPage);

        MainWindow.Activate();
    }
}
```

---

## 3. Phase 2: JSON Database (DataService) & Settings

### 3.1. DevDeckData Model (`Models/DevDeckData.cs`)
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

### 3.2. DataService (`Data/IDataService.cs` & `Data/DataService.cs`)
Handles reading and writing `data.json` atomically in the AppData directory.

`IDataService.cs`:
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

`DataService.cs`:
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

---

## 4. Phase 3: Windows 11 Shell

*Custom Title Bar & NavigationView Page Layout logic remains unchanged from original spec.*

---
