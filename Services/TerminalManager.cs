using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevDeck.Contracts;
using DevDeck.Enums;
using DevDeck.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace DevDeck.Services
{
    public sealed class TerminalManager : ITerminalManager, IDisposable
    {
        private readonly IPathResolverService _pathResolver;
        private readonly IServiceProvider _serviceProvider;
        private WebView2? _webView;
        private readonly List<TerminalSession> _sessions = [];
        private readonly ConcurrentDictionary<Guid, ITerminalBackend> _backends = new();
        private readonly ConcurrentDictionary<Guid, StringBuilder> _outputBuffers = new();
        private readonly Timer _batchTimer;
        private TerminalSession? _activeSession;
        private bool _isWebViewReady;

        public IReadOnlyList<TerminalSession> Sessions => _sessions;
        public TerminalSession? ActiveSession => _activeSession;

        public event EventHandler? SessionsChanged;
        public event EventHandler<TerminalSession?>? ActiveSessionChanged;

        public TerminalManager(IPathResolverService pathResolver, IServiceProvider serviceProvider)
        {
            _pathResolver = pathResolver;
            _serviceProvider = serviceProvider;
            _batchTimer = new Timer(FlushBuffers, null, 25, 25);
        }

        public void SetWebView(WebView2 webView)
        {
            if (_webView != null)
            {
                _webView.WebMessageReceived -= OnWebMessageReceived;
            }

            _webView = webView;
            if (_webView != null)
            {
                _webView.WebMessageReceived += OnWebMessageReceived;
            }
        }

        public async Task<TerminalSession> CreateSessionAsync(Guid? projectId, string title, ShellType shellType, string workingDirectory)
        {
            var sessionId = Guid.NewGuid();
            var session = new TerminalSession
            {
                Id = sessionId,
                ProjectId = projectId,
                Title = title,
                Shell = shellType,
                WorkingDirectory = workingDirectory,
                State = RunState.Running
            };

            _sessions.Add(session);
            SessionsChanged?.Invoke(this, EventArgs.Empty);

            // Create backend
            var backend = (ITerminalBackend?)_serviceProvider.GetService(typeof(ITerminalBackend)) ?? new ConPtyTerminalBackend();
            _backends[sessionId] = backend;

            backend.OutputReceived += (s, data) => OnBackendOutputReceived(sessionId, data);
            backend.Exited += (s, code) => OnBackendExited(sessionId, code);

            string shellPath = GetShellPath(shellType);
            string args = shellType switch
            {
                ShellType.GitBash => "--login -i",
                _ => string.Empty
            };

            // Start backend in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    await backend.StartAsync(shellPath, args, workingDirectory, 80, 24, session.Cancellation.Token);
                    
                    // If webview is already ready, tell it to create the term session
                    if (_isWebViewReady)
                    {
                        SendToWeb(new { type = "createSession", sessionId = sessionId.ToString(), title = title });
                    }
                }
                catch (Exception)
                {
                    session.State = RunState.Failed;
                }
            });

            await ActivateSessionAsync(sessionId);
            return session;
        }

        public async Task CloseSessionAsync(Guid sessionId)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                session.Cancellation.Cancel();
                _sessions.Remove(session);
                SessionsChanged?.Invoke(this, EventArgs.Empty);

                if (_backends.TryRemove(sessionId, out var backend))
                {
                    await backend.StopAsync(true, CancellationToken.None);
                    await backend.DisposeAsync();
                }

                _outputBuffers.TryRemove(sessionId, out _);

                SendToWeb(new { type = "closeSession", sessionId = sessionId.ToString() });

                if (_activeSession?.Id == sessionId)
                {
                    var nextSession = _sessions.LastOrDefault();
                    if (nextSession != null)
                    {
                        await ActivateSessionAsync(nextSession.Id);
                    }
                    else
                    {
                        _activeSession = null;
                        ActiveSessionChanged?.Invoke(this, null);
                    }
                }
            }
        }

        public async Task ActivateSessionAsync(Guid sessionId)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                _activeSession = session;
                ActiveSessionChanged?.Invoke(this, _activeSession);

                if (_isWebViewReady)
                {
                    SendToWeb(new { type = "activateSession", sessionId = sessionId.ToString() });
                }
            }
            await Task.CompletedTask;
        }

        public async Task SendInputAsync(Guid sessionId, string data)
        {
            if (_backends.TryGetValue(sessionId, out var backend))
            {
                await backend.WriteAsync(data, CancellationToken.None);
            }
        }

        public async Task ClearActiveSessionAsync()
        {
            if (_activeSession != null)
            {
                // Send clear key sequence (Ctrl+L or ANSI escape code to clear screen)
                // xterm.js supports clear through ESC [ 2 J and ESC [ H
                SendToWeb(new { type = "output", sessionId = _activeSession.Id.ToString(), data = "\x1b[2J\x1b[H" });
            }
            await Task.CompletedTask;
        }

        public async Task KillActiveSessionAsync()
        {
            if (_activeSession != null)
            {
                var id = _activeSession.Id;
                await CloseSessionAsync(id);
            }
        }

        private void OnBackendOutputReceived(Guid sessionId, string data)
        {
            var buffer = _outputBuffers.GetOrAdd(sessionId, _ => new StringBuilder());
            lock (buffer)
            {
                buffer.Append(data);
            }
        }

        private void OnBackendExited(Guid sessionId, int code)
        {
            var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
            if (session != null)
            {
                session.State = code == 0 ? RunState.Succeeded : RunState.Failed;
                // Output exit notice
                OnBackendOutputReceived(sessionId, $"\r\n[Process exited with code {code}]\r\n");
            }
        }

        private void FlushBuffers(object? state)
        {
            if (!_isWebViewReady || _webView == null) return;

            foreach (var pair in _outputBuffers)
            {
                var sessionId = pair.Key;
                var buffer = pair.Value;
                string dataToSend = string.Empty;

                lock (buffer)
                {
                    if (buffer.Length > 0)
                    {
                        dataToSend = buffer.ToString();
                        buffer.Clear();
                    }
                }

                if (!string.IsNullOrEmpty(dataToSend))
                {
                    SendToWeb(new { type = "output", sessionId = sessionId.ToString(), data = dataToSend });
                }
            }
        }

        private void SendToWeb(object msg)
        {
            if (_webView == null || !_isWebViewReady) return;

            try
            {
                string json = JsonSerializer.Serialize(msg);
                _webView.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        _webView.CoreWebView2?.PostWebMessageAsJson(json);
                    }
                    catch { }
                });
            }
            catch { }
        }

        private void OnWebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string json = args.WebMessageAsJson;
                HandleWebMessage(json);
            }
            catch { }
        }

        public void HandleWebMessage(string jsonMessage)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonMessage);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) return;

                string type = typeProp.GetString() ?? string.Empty;

                if (type == "ready")
                {
                    _isWebViewReady = true;
                    // Restore active session visual
                    if (_activeSession != null)
                    {
                        SendToWeb(new { type = "createSession", sessionId = _activeSession.Id.ToString(), title = _activeSession.Title });
                        SendToWeb(new { type = "activateSession", sessionId = _activeSession.Id.ToString() });
                    }
                    
                    // Create other sessions in webview if any exist
                    foreach (var s in _sessions)
                    {
                        if (s.Id != _activeSession?.Id)
                        {
                            SendToWeb(new { type = "createSession", sessionId = s.Id.ToString(), title = s.Title });
                        }
                    }
                }
                else if (type == "input")
                {
                    string sIdStr = root.GetProperty("sessionId").GetString() ?? string.Empty;
                    string data = root.GetProperty("data").GetString() ?? string.Empty;
                    if (Guid.TryParse(sIdStr, out var sessionId))
                    {
                        _ = SendInputAsync(sessionId, data);
                    }
                }
                else if (type == "resize")
                {
                    string sIdStr = root.GetProperty("sessionId").GetString() ?? string.Empty;
                    int cols = root.GetProperty("cols").GetInt32();
                    int rows = root.GetProperty("rows").GetInt32();
                    if (Guid.TryParse(sIdStr, out var sessionId))
                    {
                        if (_backends.TryGetValue(sessionId, out var backend))
                        {
                            _ = backend.ResizeAsync(cols, rows, CancellationToken.None);
                        }
                    }
                }
            }
            catch { }
        }

        private string GetShellPath(ShellType shell)
        {
            return shell switch
            {
                ShellType.PowerShell7 => "pwsh.exe",
                ShellType.WindowsPowerShell => "powershell.exe",
                ShellType.CommandPrompt => "cmd.exe",
                ShellType.GitBash => GetGitBashPath(),
                _ => "cmd.exe"
            };
        }

        private string GetGitBashPath()
        {
            string path1 = @"C:\Program Files\Git\bin\bash.exe";
            if (File.Exists(path1)) return path1;
            string path2 = @"C:\Program Files\Git\git-bash.exe";
            if (File.Exists(path2)) return path2;
            return "bash.exe";
        }

        public void Dispose()
        {
            _batchTimer.Dispose();
            foreach (var backend in _backends.Values)
            {
                try
                {
                    _ = backend.StopAsync(true, CancellationToken.None);
                }
                catch { }
            }
        }
    }
}
