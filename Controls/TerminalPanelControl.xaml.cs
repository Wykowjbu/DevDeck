using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Contracts;
using DevDeck.Models;
using DevDeck.Enums;
using DevDeck.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;

namespace DevDeck.Controls
{
    public sealed partial class TerminalPanelControl : UserControl
    {
        public event RoutedEventHandler? HideClicked;

        private readonly ITerminalManager _terminalManager;
        private readonly AppStateService _appStateService;
        private readonly IWorkspaceService _workspaceService;
        private readonly IDialogService _dialogService;
        private bool _isWebViewInitialized = false;

        public TerminalPanelControl()
        {
            InitializeComponent();
            _terminalManager = App.AppHost.Services.GetRequiredService<ITerminalManager>();
            _appStateService = App.AppHost.Services.GetRequiredService<AppStateService>();
            _workspaceService = App.AppHost.Services.GetRequiredService<IWorkspaceService>();
            _dialogService = App.AppHost.Services.GetRequiredService<IDialogService>();

            _terminalManager.SessionsChanged += TerminalManager_SessionsChanged;
            _terminalManager.ActiveSessionChanged += TerminalManager_ActiveSessionChanged;

            this.Loaded += TerminalPanelControl_Loaded;
        }

        private void TerminalPanelControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeWebViewOnce();
            UpdateTabsList();
        }

        private async void InitializeWebViewOnce()
        {
            if (_isWebViewInitialized) return;
            _isWebViewInitialized = true;

            try
            {
                // Set WebView2 environment and mapping
                await TerminalWebView.EnsureCoreWebView2Async();
                _terminalManager.SetWebView(TerminalWebView);

                // Map virtual host directory
                string appDataDir = AppContext.BaseDirectory;
                string localFolder = Path.Combine(appDataDir, "Assets", "Terminal");
                if (!Directory.Exists(localFolder))
                {
                    // Fallback to project root directory during dev if run from bin
                    localFolder = Path.Combine(Directory.GetParent(appDataDir)!.Parent!.Parent!.FullName, "Assets", "Terminal");
                }

                TerminalWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    "terminal.local",
                    localFolder,
                    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);

                TerminalWebView.Source = new Uri("http://terminal.local/index.html");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 Error: {ex.Message}");
            }
        }

        private void TerminalWebView_NavigationCompleted(WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            UpdateTabsList();
        }

        private void TerminalManager_SessionsChanged(object? sender, EventArgs e)
        {
            UpdateTabsList();
        }

        private void TerminalManager_ActiveSessionChanged(object? sender, TerminalSession? activeSession)
        {
            if (activeSession != null)
            {
                TabsListView.SelectedItem = activeSession;
            }
            else
            {
                TabsListView.SelectedItem = null;
            }
            UpdateTabsList();
        }

        private void UpdateTabsList()
        {
            TabsListView.ItemsSource = null;
            TabsListView.ItemsSource = _terminalManager.Sessions;

            if (_terminalManager.ActiveSession != null)
            {
                TabsListView.SelectedItem = _terminalManager.ActiveSession;
            }

            bool hasSessions = _terminalManager.Sessions.Count > 0;
            TerminalWebView.Visibility = hasSessions ? Visibility.Visible : Visibility.Collapsed;
            NoSessionsText.Visibility = hasSessions ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void TabsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabsListView.SelectedItem is TerminalSession session)
            {
                if (_terminalManager.ActiveSession?.Id != session.Id)
                {
                    await _terminalManager.ActivateSessionAsync(session.Id);
                }
            }
        }

        private async void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TerminalSession session)
            {
                await _terminalManager.CloseSessionAsync(session.Id);
            }
        }

        private async void NewTabButton_Click(object sender, RoutedEventArgs e)
        {
            // Resolve project working dir
            string workDir = AppContext.BaseDirectory;
            Guid? projId = null;

            if (_appStateService.CurrentProjectId.HasValue)
            {
                var workspaces = await _workspaceService.GetWorkspacesAsync();
                foreach (var ws in workspaces)
                {
                    var proj = ws.Projects.FirstOrDefault(p => p.Id == _appStateService.CurrentProjectId.Value);
                    if (proj != null)
                    {
                        workDir = proj.FolderPath;
                        projId = proj.Id;
                        break;
                    }
                }
            }

            // Default shell type from AppSettings is CMD
            var settingsService = App.AppHost.Services.GetRequiredService<ISettingsService>();
            ShellType defaultShell = settingsService.Settings.DefaultShell;

            int index = _terminalManager.Sessions.Count + 1;
            string title = defaultShell switch
            {
                ShellType.PowerShell7 => $"PowerShell {index}",
                ShellType.WindowsPowerShell => $"PowerShell {index}",
                ShellType.CommandPrompt => $"CMD {index}",
                ShellType.GitBash => $"Git Bash {index}",
                _ => $"Terminal {index}"
            };

            await _terminalManager.CreateSessionAsync(projId, title, defaultShell, workDir);
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            await _terminalManager.ClearActiveSessionAsync();
        }

        private async void KillButton_Click(object sender, RoutedEventArgs e)
        {
            if (_terminalManager.ActiveSession == null)
            {
                return;
            }

            bool confirm = await _dialogService.ShowConfirmationAsync(
                "Dừng tiến trình terminal",
                $"Bạn có chắc chắn muốn dừng tiến trình trong tab '{_terminalManager.ActiveSession.Title}'? Thao tác này khác với ẩn panel terminal và có thể làm mất tác vụ đang chạy.",
                XamlRoot);

            if (confirm)
            {
                await _terminalManager.KillActiveSessionAsync();
            }
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            HideClicked?.Invoke(this, e);
        }
    }
}
