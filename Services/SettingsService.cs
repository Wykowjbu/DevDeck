using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DevDeck.Contracts;
using DevDeck.Models;

namespace DevDeck.Services
{
    public sealed class SettingsService : ISettingsService
    {
        private readonly IPathResolverService _pathResolver;
        private readonly ILogger<SettingsService> _logger;
        private AppSettings _settings = new();

        public AppSettings Settings => _settings;

        public SettingsService(IPathResolverService pathResolver, ILogger<SettingsService> logger)
        {
            _pathResolver = pathResolver;
            _logger = logger;
        }

        public async Task LoadAsync()
        {
            string path = _pathResolver.GetSettingsPath();
            if (!File.Exists(path))
            {
                _settings = new AppSettings();
                return;
            }

            try
            {
                string json = await File.ReadAllTextAsync(path);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings. Loading defaults.");
                _settings = new AppSettings();
            }
        }

        public async Task SaveAsync()
        {
            string path = _pathResolver.GetSettingsPath();
            string tempPath = path + ".tmp";

            try
            {
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(tempPath, json);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings atomically.");
            }
        }
    }
}
