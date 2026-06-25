using System;
using System.IO;
using DevDeck.Contracts;

namespace DevDeck.Services
{
    public sealed class PathResolverService : IPathResolverService
    {
        private readonly string _baseDir;

        public PathResolverService()
        {
            _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevDeck");
            Directory.CreateDirectory(_baseDir);
            Directory.CreateDirectory(GetIconsDirectory());
            Directory.CreateDirectory(GetLogsDirectory());
        }

        public string GetAppDataDirectory() => _baseDir;
        public string GetDatabasePath() => Path.Combine(_baseDir, "devdeck.db");
        public string GetSettingsPath() => Path.Combine(_baseDir, "settings.json");
        public string GetIconsDirectory() => Path.Combine(_baseDir, "icons");
        public string GetLogsDirectory() => Path.Combine(_baseDir, "logs");
    }
}
