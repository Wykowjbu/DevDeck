using System.Threading.Tasks;
using DevDeck.Models;

namespace DevDeck.Contracts
{
    public interface ISettingsService
    {
        AppSettings Settings { get; }
        Task LoadAsync();
        Task SaveAsync();
    }
}
