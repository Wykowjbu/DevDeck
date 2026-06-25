using System;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class AppSettings
    {
        public AppTheme Theme { get; set; } = AppTheme.System;
        public BackdropKind Backdrop { get; set; } = BackdropKind.Mica;
        public ShellType DefaultShell { get; set; } = ShellType.CommandPrompt;
        public string TerminalFontFamily { get; set; } = "Cascadia Mono";
        public double TerminalFontSize { get; set; } = 13;
        public double TerminalPanelHeight { get; set; } = 260;
        public ActionButtonSize ActionButtonSize { get; set; } = ActionButtonSize.Standard;
        public Guid? LastWorkspaceId { get; set; }
        public Guid? LastProjectId { get; set; }
        public bool RestoreTerminalVisibility { get; set; }
        public bool TerminalWasVisible { get; set; }
        public int MaximumConcurrentActions { get; set; } = 4;
    }
}
