using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class ProjectActionEntity
    {
        public Guid ProjectId { get; set; }
        public Guid DevActionId { get; set; }
        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; }
        public string? NameOverride { get; set; }
        public IconKind? IconKindOverride { get; set; }
        public string? IconValueOverride { get; set; }
        public string? GroupNameOverride { get; set; }
        public bool? RequireConfirmationOverride { get; set; }
        public bool? StopOnFailureOverride { get; set; }
        public bool? AllowConcurrentRunsOverride { get; set; }
        [JsonIgnore]
        public ProjectEntity Project { get; set; } = null!;
        [JsonIgnore]
        public DevActionEntity DevAction { get; set; } = null!;
        public ICollection<ProjectActionStepOverrideEntity> StepOverrides { get; set; } = [];
    }
}
