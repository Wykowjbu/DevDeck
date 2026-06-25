using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DevDeck.Models;
using DevDeck.Enums;
using System;
using System.IO;
using DevDeck.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Automation;

namespace DevDeck.Controls
{
    public sealed partial class ActionButtonControl : UserControl
    {
        public static readonly DependencyProperty ProjectActionProperty =
            DependencyProperty.Register(
                nameof(ProjectAction),
                typeof(ProjectActionEntity),
                typeof(ActionButtonControl),
                new PropertyMetadata(null, OnProjectActionChanged));

        public ProjectActionEntity? ProjectAction
        {
            get => (ProjectActionEntity?)GetValue(ProjectActionProperty);
            set => SetValue(ProjectActionProperty, value);
        }

        public event EventHandler<ProjectActionEntity>? RunClicked;
        public event EventHandler<ProjectActionEntity>? StopClicked;
        public event EventHandler<ProjectActionEntity>? RemoveClicked;
        public event EventHandler<ProjectActionEntity>? CustomizeClicked;
        public event EventHandler<ProjectActionEntity>? ResetClicked;

        private RunState _currentState = RunState.Idle;

        public ActionButtonControl()
        {
            InitializeComponent();
        }

        private static void OnProjectActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ActionButtonControl control)
            {
                control.UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (ProjectAction == null) return;

            // Resolve name (respect project override if available)
            string displayName = ProjectAction.NameOverride ?? ProjectAction.DevAction?.Name ?? "Unnamed Action";
            ActionNameText.Text = displayName;
            AutomationProperties.SetName(ActionButton, $"Chạy Action {displayName}");
            ToolTipService.SetToolTip(ActionButton, $"Chạy Action {displayName}");

            // Load and cache custom icon if using LocalFile
            var iconKind = ProjectAction.IconKindOverride ?? ProjectAction.DevAction?.IconKind ?? IconKind.FluentGlyph;
            var iconValue = ProjectAction.IconValueOverride ?? ProjectAction.DevAction?.IconValue;

            if (iconKind == IconKind.LocalFile && !string.IsNullOrEmpty(iconValue))
            {
                try
                {
                    var pathResolver = App.AppHost.Services.GetRequiredService<IPathResolverService>();
                    string fullPath = Path.Combine(pathResolver.GetAppDataDirectory(), iconValue);
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(fullPath));
                        CustomIconImage.Source = bitmap;
                    }
                }
                catch
                {
                    // Ignore and let SetState handle fallback
                }
            }

            SetState(RunState.Idle);
        }

        public void SetState(RunState state)
        {
            _currentState = state;

            // Reset visibilities
            SuccessIcon.Visibility = Visibility.Collapsed;
            FailedIcon.Visibility = Visibility.Collapsed;
            RunningRing.Visibility = Visibility.Collapsed;
            RunningRing.IsActive = false;

            var iconKind = ProjectAction?.IconKindOverride ?? ProjectAction?.DevAction?.IconKind ?? IconKind.FluentGlyph;
            var iconValue = ProjectAction?.IconValueOverride ?? ProjectAction?.DevAction?.IconValue;

            bool showActionIcon = (state == RunState.Idle || state == RunState.Stopped);

            DefaultSymbolIcon.Visibility = Visibility.Collapsed;
            CustomIconImage.Visibility = Visibility.Collapsed;
            DefaultIcon.Visibility = Visibility.Collapsed;

            if (showActionIcon)
            {
                AutomationProperties.SetHelpText(ActionButton, "Sẵn sàng chạy");

                if (iconKind == IconKind.FluentGlyph && !string.IsNullOrEmpty(iconValue) && Enum.TryParse<Symbol>(iconValue, out var symbol))
                {
                    DefaultSymbolIcon.Symbol = symbol;
                    DefaultSymbolIcon.Visibility = Visibility.Visible;
                }
                else if (iconKind == IconKind.LocalFile && !string.IsNullOrEmpty(iconValue) && CustomIconImage.Source != null)
                {
                    CustomIconImage.Visibility = Visibility.Visible;
                }
                else
                {
                    DefaultIcon.Glyph = "\uE768";
                    DefaultIcon.Visibility = Visibility.Visible;
                }
            }
            else
            {
                switch (state)
                {
                    case RunState.Running:
                        RunningRing.Visibility = Visibility.Visible;
                        RunningRing.IsActive = true;
                        AutomationProperties.SetHelpText(ActionButton, "Action đang chạy");
                        break;

                    case RunState.Succeeded:
                        SuccessIcon.Visibility = Visibility.Visible;
                        AutomationProperties.SetHelpText(ActionButton, "Action đã chạy thành công");
                        _ = RestoreToIdleAfterDelayAsync();
                        break;

                    case RunState.Failed:
                        FailedIcon.Visibility = Visibility.Visible;
                        AutomationProperties.SetHelpText(ActionButton, "Action chạy thất bại");
                        _ = RestoreToIdleAfterDelayAsync();
                        break;
                }
            }
        }

        private async System.Threading.Tasks.Task RestoreToIdleAfterDelayAsync()
        {
            await System.Threading.Tasks.Task.Delay(3000);
            if (_currentState == RunState.Succeeded || _currentState == RunState.Failed)
            {
                SetState(RunState.Idle);
            }
        }

        private void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectAction != null && _currentState != RunState.Running)
            {
                RunClicked?.Invoke(this, ProjectAction);
            }
        }

        private void RunAction_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectAction != null)
            {
                RunClicked?.Invoke(this, ProjectAction);
            }
        }

        private void StopAction_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectAction != null)
            {
                StopClicked?.Invoke(this, ProjectAction);
            }
        }

        private void RemoveFromProject_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectAction != null)
            {
                RemoveClicked?.Invoke(this, ProjectAction);
            }
        }

        private void Customize_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectAction != null)
            {
                CustomizeClicked?.Invoke(this, ProjectAction);
            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectAction != null)
            {
                ResetClicked?.Invoke(this, ProjectAction);
            }
        }
    }
}
