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
