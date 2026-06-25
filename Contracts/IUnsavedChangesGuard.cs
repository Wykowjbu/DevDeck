using System.Threading.Tasks;

namespace DevDeck.Contracts
{
    public interface IUnsavedChangesGuard
    {
        bool HasUnsavedChanges { get; }
        Task<bool> ConfirmLeaveAsync();
    }
}
