using System;
using System.Text.Json.Serialization;
using DevDeck.Enums;

namespace DevDeck.Models
{
    public sealed class ActionStepEntity
    {
        public Guid Id { get; set; }
        public Guid DevActionId { get; set; }
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
        [JsonIgnore]
        public DevActionEntity DevAction { get; set; } = null!;

        public override string ToString()
        {
            return StepType switch
            {
                ActionStepType.ShellCommand => $"Bước {SortOrder}: {CommandText}",
                ActionStepType.Delay => $"Bước {SortOrder}: Chờ {DelayMilliseconds ?? 1000} ms",
                ActionStepType.LaunchApplication => $"Bước {SortOrder}: Mở ứng dụng",
                ActionStepType.OpenFolder => $"Bước {SortOrder}: Mở thư mục",
                ActionStepType.OpenFile => $"Bước {SortOrder}: Mở file",
                ActionStepType.OpenUrl => $"Bước {SortOrder}: Mở URL",
                _ => $"Bước {SortOrder}: {StepType}"
            };
        }
    }
}
