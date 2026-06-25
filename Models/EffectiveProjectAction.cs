using System;
using System.Collections.Generic;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class EffectiveProjectAction
    {
        public Guid ProjectId { get; set; }
        public Guid DevActionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public IconKind IconKind { get; set; }
        public string? IconValue { get; set; }
        public string? GroupName { get; set; }
        public bool RequireConfirmation { get; set; }
        public bool StopOnFailure { get; set; } = true;
        public bool AllowConcurrentRuns { get; set; }
        public ActionExecutionMode ExecutionMode { get; set; }
        public IReadOnlyList<EffectiveActionStep> Steps { get; set; } = [];
    }
}
