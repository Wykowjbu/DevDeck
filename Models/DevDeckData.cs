using System.Collections.Generic;

namespace DevDeck.Models
{
    public sealed class DevDeckData
    {
        public List<WorkspaceEntity> Workspaces { get; set; } = [];
        public List<DevActionEntity> GlobalActions { get; set; } = [];
    }
}
