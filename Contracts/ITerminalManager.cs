using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DevDeck.Models;
using DevDeck.Enums;
using Microsoft.UI.Xaml.Controls;

namespace DevDeck.Contracts
{
    public interface ITerminalManager
    {
        IReadOnlyList<TerminalSession> Sessions { get; }
        TerminalSession? ActiveSession { get; }

        event EventHandler? SessionsChanged;
        event EventHandler<TerminalSession?>? ActiveSessionChanged;

        void SetWebView(WebView2 webView);
        Task<TerminalSession> CreateSessionAsync(Guid? projectId, string title, ShellType shellType, string workingDirectory);
        Task CloseSessionAsync(Guid sessionId);
        Task ActivateSessionAsync(Guid sessionId);
        Task SendInputAsync(Guid sessionId, string data);
        Task ClearActiveSessionAsync();
        Task KillActiveSessionAsync();
        void HandleWebMessage(string jsonMessage);
    }
}
