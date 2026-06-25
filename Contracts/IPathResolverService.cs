namespace DevDeck.Contracts
{
    public interface IPathResolverService
    {
        string GetAppDataDirectory();
        string GetDatabasePath();
        string GetSettingsPath();
        string GetIconsDirectory();
        string GetLogsDirectory();
    }
}
