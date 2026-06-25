using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DevDeck.Contracts;
using DevDeck.Enums;
using DevDeck.Models;
using Microsoft.UI.Xaml;

namespace DevDeck.Services
{
    public sealed class ActionExecutionService : IActionExecutionService
    {
        private readonly IVariableResolver _variableResolver;
        private readonly IDialogService _dialogService;
        private readonly ActionOverrideResolver _overrideResolver;
        private readonly ISettingsService _settingsService;

        private readonly ConcurrentDictionary<(Guid ProjectId, Guid DevActionId), RunningSession> _runningSessions = new();

        public ActionExecutionService(
            IVariableResolver variableResolver,
            IDialogService dialogService,
            ActionOverrideResolver overrideResolver,
            ISettingsService settingsService)
        {
            _variableResolver = variableResolver;
            _dialogService = dialogService;
            _overrideResolver = overrideResolver;
            _settingsService = settingsService;
        }

        public bool IsRunning(Guid projectId, Guid actionId)
        {
            return _runningSessions.ContainsKey((projectId, actionId));
        }

        public async Task StopProjectActionAsync(Guid projectId, Guid actionId)
        {
            if (_runningSessions.TryGetValue((projectId, actionId), out var session))
            {
                session.Cancel();
                await session.KillProcessesAsync();
                _runningSessions.TryRemove((projectId, actionId), out _);
            }
        }

        public async Task RunProjectActionAsync(ProjectActionEntity projectAction, XamlRoot xamlRoot, Action<RunState> stateChangedHandler)
        {
            var effective = _overrideResolver.Resolve(projectAction);
            var key = (effective.ProjectId, effective.DevActionId);

            if (_runningSessions.ContainsKey(key))
            {
                if (!effective.AllowConcurrentRuns)
                {
                    await _dialogService.ShowMessageAsync("Lỗi", $"Action '{effective.Name}' hiện đang chạy trên Project này và không cấu hình cho phép chạy song song.", xamlRoot);
                    return;
                }
            }

            int maxConcurrent = Math.Max(1, _settingsService.Settings.MaximumConcurrentActions);
            if (_runningSessions.Count >= maxConcurrent)
            {
                await _dialogService.ShowMessageAsync("Đang bận", $"DevDeck đang chạy tối đa {maxConcurrent} action cùng lúc. Hãy đợi một action xong rồi chạy tiếp.", xamlRoot);
                return;
            }

            if (effective.RequireConfirmation)
            {
                bool confirm = await _dialogService.ShowConfirmationAsync(
                    "Xác nhận chạy Action",
                    $"Bạn có chắc chắn muốn chạy Action '{effective.Name}' trên Project '{projectAction.Project?.Name}'?",
                    xamlRoot);

                if (!confirm) return;
            }

            var cts = new CancellationTokenSource();
            var session = new RunningSession(cts);
            _runningSessions[key] = session;

            stateChangedHandler(RunState.Running);

            try
            {
                bool success = true;
                foreach (var step in effective.Steps)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    // Resolve working directory
                    string rawWorkDir = string.IsNullOrEmpty(step.WorkingDirectory) ? "${project.path}" : step.WorkingDirectory;
                    string resolvedWorkDir = _variableResolver.Resolve(rawWorkDir, projectAction.Project!);
                    if (!Directory.Exists(resolvedWorkDir))
                    {
                        resolvedWorkDir = projectAction.Project!.FolderPath;
                    }

                    bool stepSuccess = await ExecuteStepAsync(step, resolvedWorkDir, projectAction.Project!, session, cts.Token);
                    if (!stepSuccess)
                    {
                        if (step.StopOnFailure)
                        {
                            success = false;
                            break;
                        }
                    }
                }

                if (cts.Token.IsCancellationRequested)
                {
                    stateChangedHandler(RunState.Stopped);
                }
                else if (success)
                {
                    stateChangedHandler(RunState.Succeeded);
                }
                else
                {
                    stateChangedHandler(RunState.Failed);
                }
            }
            catch (OperationCanceledException)
            {
                stateChangedHandler(RunState.Stopped);
            }
            catch (Exception ex)
            {
                stateChangedHandler(RunState.Failed);
                LoggerHelper.LogToFile($"RunProjectAction_{effective.Name}", ex, projectAction.Project?.FolderPath);
                await _dialogService.ShowMessageAsync("Lỗi chạy Action", $"Có lỗi xảy ra khi chạy: {ex.Message}", xamlRoot);
            }
            finally
            {
                _runningSessions.TryRemove(key, out _);
                cts.Dispose();
            }
        }

        private async Task<bool> ExecuteStepAsync(EffectiveActionStep step, string workingDir, ProjectEntity project, RunningSession session, CancellationToken cancellationToken)
        {
            switch (step.StepType)
            {
                case ActionStepType.LaunchApplication:
                    return ExecuteLaunchApplication(step, workingDir, project);

                case ActionStepType.OpenFolder:
                    return ExecuteOpenFolder(step, workingDir, project);

                case ActionStepType.OpenFile:
                    return ExecuteOpenFile(step, workingDir, project);

                case ActionStepType.OpenUrl:
                    return ExecuteOpenUrl(step, project);

                case ActionStepType.Delay:
                    return await ExecuteDelayAsync(step, cancellationToken);

                case ActionStepType.ShellCommand:
                    return await ExecuteShellCommandAsync(step, workingDir, project, session, cancellationToken);

                default:
                    return false;
            }
        }

        private bool ExecuteLaunchApplication(EffectiveActionStep step, string workingDir, ProjectEntity project)
        {
            if (string.IsNullOrEmpty(step.ApplicationPath)) return false;

            string resolvedApp = _variableResolver.Resolve(step.ApplicationPath, project);
            string resolvedArgs = string.IsNullOrEmpty(step.Arguments) ? string.Empty : _variableResolver.Resolve(step.Arguments, project);

            var startInfo = new ProcessStartInfo
            {
                FileName = resolvedApp,
                Arguments = resolvedArgs,
                WorkingDirectory = workingDir,
                UseShellExecute = true
            };

            var proc = Process.Start(startInfo);
            return proc != null;
        }

        private bool ExecuteOpenFolder(EffectiveActionStep step, string workingDir, ProjectEntity project)
        {
            string folderPath = string.IsNullOrEmpty(step.TargetPath) ? workingDir : _variableResolver.Resolve(step.TargetPath, project);
            if (!Directory.Exists(folderPath)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            };

            var proc = Process.Start(startInfo);
            return proc != null;
        }

        private bool ExecuteOpenFile(EffectiveActionStep step, string workingDir, ProjectEntity project)
        {
            if (string.IsNullOrEmpty(step.TargetPath)) return false;

            string filePath = _variableResolver.Resolve(step.TargetPath, project);
            if (!File.Exists(filePath)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = filePath,
                WorkingDirectory = workingDir,
                UseShellExecute = true
            };

            var proc = Process.Start(startInfo);
            return proc != null;
        }

        private bool ExecuteOpenUrl(EffectiveActionStep step, ProjectEntity project)
        {
            if (string.IsNullOrEmpty(step.Url)) return false;

            string url = _variableResolver.Resolve(step.Url, project);
            if (!Uri.TryCreate(url, UriKind.Absolute, out _)) return false;

            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            var proc = Process.Start(startInfo);
            return proc != null;
        }

        private async Task<bool> ExecuteDelayAsync(EffectiveActionStep step, CancellationToken cancellationToken)
        {
            int ms = step.DelayMilliseconds ?? 1000;
            if (ms > 0)
            {
                await Task.Delay(ms, cancellationToken);
            }
            return true;
        }

        private async Task<bool> ExecuteShellCommandAsync(EffectiveActionStep step, string workingDir, ProjectEntity project, RunningSession session, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(step.CommandText)) return false;

            string resolvedCommand = _variableResolver.Resolve(step.CommandText, project);

            ActionOutputMode outputMode = step.OutputMode == ActionOutputMode.IntegratedTerminal
                ? ActionOutputMode.ExternalTerminal
                : step.OutputMode;

            string shellExe = step.Shell switch
            {
                ShellType.PowerShell7 => "pwsh.exe",
                ShellType.WindowsPowerShell => "powershell.exe",
                ShellType.CommandPrompt => "cmd.exe",
                ShellType.GitBash => "bash.exe",
                _ => "cmd.exe"
            };

            if (outputMode == ActionOutputMode.Silent)
            {
                string args = step.Shell switch
                {
                    ShellType.CommandPrompt => $"/d /s /c \"{resolvedCommand}\"",
                    ShellType.GitBash => $"-c \"{resolvedCommand}\"",
                    _ => $"-Command \"{resolvedCommand}\"" // powershell / pwsh
                };

                var startInfo = new ProcessStartInfo
                {
                    FileName = shellExe,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using var process = new Process { StartInfo = startInfo };
                session.AddProcess(process);

                try
                {
                    if (!process.Start()) return false;

                    using (cancellationToken.Register(() => {
                        try { process.Kill(true); } catch { }
                    }))
                    {
                        await process.WaitForExitAsync(cancellationToken);
                    }

                    return process.ExitCode == 0;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                finally
                {
                    session.RemoveProcess(process);
                }
            }
            else // ExternalTerminal fallback
            {
                string args = step.Shell switch
                {
                    ShellType.CommandPrompt => $"/k \"{resolvedCommand}\"",
                    ShellType.GitBash => $"-c \"{resolvedCommand}; read -p 'Press enter to exit...'\"",
                    _ => $"-NoExit -Command \"{resolvedCommand}\"" // powershell / pwsh
                };

                var startInfo = new ProcessStartInfo
                {
                    FileName = shellExe,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    UseShellExecute = true
                };

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    session.AddProcess(process);
                    // For external terminal, we don't block the sequential execution unless desired.
                    // But usually, an external terminal is spawned and runs asynchronously.
                    // However, we wait a tiny bit to make sure it spawns.
                    await Task.Delay(200, cancellationToken);
                    return true;
                }
                return false;
            }
        }

        private sealed class RunningSession
        {
            private readonly CancellationTokenSource _cts;
            private readonly List<Process> _activeProcesses = [];
            private readonly object _lock = new();

            public RunningSession(CancellationTokenSource cts)
            {
                _cts = cts;
            }

            public void Cancel()
            {
                try
                {
                    _cts.Cancel();
                }
                catch { }
            }

            public void AddProcess(Process p)
            {
                lock (_lock)
                {
                    _activeProcesses.Add(p);
                }
            }

            public void RemoveProcess(Process p)
            {
                lock (_lock)
                {
                    _activeProcesses.Remove(p);
                }
            }

            public async Task KillProcessesAsync()
            {
                List<Process> list;
                lock (_lock)
                {
                    list = [.. _activeProcesses];
                }

                foreach (var p in list)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            // Kill process tree
                            using var killer = Process.Start(new ProcessStartInfo
                            {
                                FileName = "taskkill",
                                Arguments = $"/F /T /PID {p.Id}",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            if (killer != null)
                            {
                                await killer.WaitForExitAsync();
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
