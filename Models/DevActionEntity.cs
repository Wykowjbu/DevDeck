using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class DevActionEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IconKind IconKind { get; set; }
        public string? IconValue { get; set; }
        public ActionScope Scope { get; set; }
        public Guid? OwnerWorkspaceId { get; set; }
        public Guid? OwnerProjectId { get; set; }
        public string? GroupName { get; set; }
        public int SortOrder { get; set; }
        public ActionExecutionMode ExecutionMode { get; set; }
        public bool StopOnFailure { get; set; } = true;
        public bool RequireConfirmation { get; set; }
        public bool AllowConcurrentRuns { get; set; }
        public ICollection<ActionStepEntity> Steps { get; set; } = [];
        [JsonIgnore]
        public ICollection<ProjectActionEntity> ProjectActions { get; set; } = [];
    }
}
