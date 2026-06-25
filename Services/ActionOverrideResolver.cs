using System;
using System.Collections.Generic;
using System.Linq;
using DevDeck.Models;
using DevDeck.Enums;

namespace DevDeck.Services
{
    public sealed class ActionOverrideResolver
    {
        public EffectiveProjectAction Resolve(ProjectActionEntity entity)
        {
            var globalAction = entity.DevAction;

            var effective = new EffectiveProjectAction
            {
                ProjectId = entity.ProjectId,
                DevActionId = entity.DevActionId,
                Name = entity.NameOverride ?? globalAction?.Name ?? "Unnamed Action",
                IconKind = entity.IconKindOverride ?? globalAction?.IconKind ?? IconKind.FluentGlyph,
                IconValue = entity.IconValueOverride ?? globalAction?.IconValue,
                GroupName = entity.GroupNameOverride ?? globalAction?.GroupName,
                RequireConfirmation = entity.RequireConfirmationOverride ?? globalAction?.RequireConfirmation ?? false,
                StopOnFailure = entity.StopOnFailureOverride ?? globalAction?.StopOnFailure ?? true,
                AllowConcurrentRuns = entity.AllowConcurrentRunsOverride ?? globalAction?.AllowConcurrentRuns ?? false,
                ExecutionMode = globalAction?.ExecutionMode ?? ActionExecutionMode.Sequential
            };

            var steps = new List<EffectiveActionStep>();
            if (globalAction != null)
            {
                foreach (var step in globalAction.Steps.OrderBy(s => s.SortOrder))
                {
                    var stepOverride = entity.StepOverrides.FirstOrDefault(o => o.ActionStepId == step.Id);

                    steps.Add(new EffectiveActionStep
                    {
                        Id = step.Id,
                        StepType = step.StepType,
                        SortOrder = step.SortOrder,
                        CommandText = stepOverride?.CommandTextOverride ?? step.CommandText,
                        ApplicationPath = stepOverride?.ApplicationPathOverride ?? step.ApplicationPath,
                        Arguments = stepOverride?.ArgumentsOverride ?? step.Arguments,
                        TargetPath = stepOverride?.TargetPathOverride ?? step.TargetPath,
                        Url = stepOverride?.UrlOverride ?? step.Url,
                        WorkingDirectory = stepOverride?.WorkingDirectoryOverride ?? step.WorkingDirectory,
                        Shell = stepOverride?.ShellOverride ?? step.Shell,
                        OutputMode = stepOverride?.OutputModeOverride ?? step.OutputMode,
                        DelayMilliseconds = stepOverride?.DelayMillisecondsOverride ?? step.DelayMilliseconds,
                        StopOnFailure = stepOverride?.StopOnFailureOverride ?? step.StopOnFailure
                    });
                }
            }

            effective.Steps = steps;
            return effective;
        }
    }
}
