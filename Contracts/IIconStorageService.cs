using System.Threading.Tasks;

namespace DevDeck.Contracts
{
    public interface IIconStorageService
    {
        Task<string> SaveCustomIconAsync(string sourceFilePath);
    }
}
