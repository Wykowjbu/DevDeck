using System;
using System.Collections.Generic;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class WorkspaceEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IconKind IconKind { get; set; }
        public string? IconValue { get; set; }
        public string? AccentColor { get; set; }
        public int SortOrder { get; set; }
        public ICollection<ProjectEntity> Projects { get; set; } = [];
    }
}
