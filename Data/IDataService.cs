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
