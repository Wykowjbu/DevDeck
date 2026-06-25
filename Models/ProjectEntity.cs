using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class ProjectEntity
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public IconKind IconKind { get; set; }
        public string? IconValue { get; set; }
        public ShellType DefaultShell { get; set; }
        public bool IsPinned { get; set; }
        public int SortOrder { get; set; }
        [JsonIgnore]
        public WorkspaceEntity Workspace { get; set; } = null!;
        public ICollection<ProjectActionEntity> ProjectActions { get; set; } = [];
    }
}
