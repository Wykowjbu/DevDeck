using System;
using System.Threading;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class TerminalSession
    {
        public Guid Id { get; init; }
        public Guid? ProjectId { get; init; }
        public string Title { get; set; } = string.Empty;
        public ShellType Shell { get; init; }
        public string WorkingDirectory { get; init; } = string.Empty;
        public RunState State { get; set; }
        public bool IsInteractive { get; init; }
        public CancellationTokenSource Cancellation { get; } = new();
    }
}
