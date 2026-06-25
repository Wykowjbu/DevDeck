using System;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class ProjectActionStepOverrideEntity
    {
        public Guid ProjectId { get; set; }
        public Guid DevActionId { get; set; }
        public Guid ActionStepId { get; set; }
        public string? CommandTextOverride { get; set; }
        public string? ApplicationPathOverride { get; set; }
        public string? ArgumentsOverride { get; set; }
        public string? TargetPathOverride { get; set; }
        public string? UrlOverride { get; set; }
        public string? WorkingDirectoryOverride { get; set; }
        public ShellType? ShellOverride { get; set; }
        public ActionOutputMode? OutputModeOverride { get; set; }
        public int? DelayMillisecondsOverride { get; set; }
        public bool? StopOnFailureOverride { get; set; }
    }
}
