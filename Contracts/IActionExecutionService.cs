using System;
using System.Threading.Tasks;
using DevDeck.Models;
using DevDeck.Enums;
using Microsoft.UI.Xaml;

namespace DevDeck.Contracts
{
    public interface IActionExecutionService
    {
        Task RunProjectActionAsync(ProjectActionEntity projectAction, XamlRoot xamlRoot, Action<RunState> stateChangedHandler);
        Task StopProjectActionAsync(Guid projectId, Guid actionId);
        bool IsRunning(Guid projectId, Guid actionId);
    }
}
