using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DevDeck.Services
{
    public sealed partial class AppStateService : ObservableObject
    {
        [ObservableProperty]
        private Guid? _currentWorkspaceId;

        [ObservableProperty]
        private Guid? _currentProjectId;

        [ObservableProperty]
        private bool _isTerminalVisible;
    }
}
