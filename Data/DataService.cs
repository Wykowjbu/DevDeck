using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevDeck.Contracts;
using DevDeck.Models;
using DevDeck.Services;

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
                RehydrateNavigationReferences();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load data.json. Initializing empty data.");
                LoggerHelper.LogToFile("DataService.LoadAsync", ex);
                _data = new DevDeckData();
            }
        }

        private void RehydrateNavigationReferences()
        {
            foreach (var action in _data.GlobalActions)
            {
                action.ProjectActions.Clear();
                foreach (var step in action.Steps)
                {
                    step.DevActionId = action.Id;
                    step.DevAction = action;
                }
            }

            foreach (var workspace in _data.Workspaces)
            {
                foreach (var project in workspace.Projects)
                {
                    project.WorkspaceId = workspace.Id;
                    project.Workspace = workspace;

                    foreach (var projectAction in project.ProjectActions)
                    {
                        projectAction.ProjectId = project.Id;
                        projectAction.Project = project;

                        var action = _data.GlobalActions.FirstOrDefault(a => a.Id == projectAction.DevActionId);
                        if (action != null)
                        {
                            projectAction.DevAction = action;
                            action.ProjectActions.Add(projectAction);
                        }
                    }
                }
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
                File.Move(tempPath, path, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save data.json atomically.");
                LoggerHelper.LogToFile("DataService.SaveAsync", ex);
            }
        }
    }
}
