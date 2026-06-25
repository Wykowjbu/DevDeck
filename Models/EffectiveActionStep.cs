using System;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class EffectiveActionStep
    {
        public Guid Id { get; set; }
        public ActionStepType StepType { get; set; }
        public int SortOrder { get; set; }
        public string? CommandText { get; set; }
        public string? ApplicationPath { get; set; }
        public string? Arguments { get; set; }
        public string? TargetPath { get; set; }
        public string? Url { get; set; }
        public string WorkingDirectory { get; set; } = "${project.path}";
        public ShellType Shell { get; set; }
        public ActionOutputMode OutputMode { get; set; }
        public int? DelayMilliseconds { get; set; }
        public bool StopOnFailure { get; set; } = true;
    }
}
