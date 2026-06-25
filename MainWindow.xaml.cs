using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System;
using DevDeck.Enums;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;

namespace DevDeck
{
    public sealed partial class MainWindow : Window
    {
        private AppWindow? _appWindow;
        private bool _isCloseConfirmed;

        public MainWindow()
        {
            InitializeComponent();
            
            // Extend window content into title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(myWndId);
            
            if (_appWindow != null)
            {
                _appWindow.Title = "DevDeck";
                _appWindow.Closing += AppWindow_Closing;
            }

            ApplyBackdrop(BackdropKind.Mica);
        }

        public void SetBreadcrumb(string text)
        {
            TitleBreadcrumb.Text = text;
        }

        public Shell.ShellPage? ShellPage => RootFrame.Content as Shell.ShellPage;

        public void NavigateToShellPage(UIElement shellPageContent)
        {
            RootFrame.Content = shellPageContent;
        }

        public void ApplyBackdrop(BackdropKind backdrop)
        {
            SystemBackdrop = backdrop switch
            {
                BackdropKind.MicaAlt => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                BackdropKind.Solid => null,
                _ => new MicaBackdrop { Kind = MicaKind.Base }
            };

            if (backdrop == BackdropKind.Solid &&
                Application.Current.Resources.TryGetValue("ApplicationPageBackgroundThemeBrush", out var backgroundBrush) &&
                backgroundBrush is Brush brush)
            {
                RootGrid.Background = brush;
            }
            else
            {
                RootGrid.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }

        private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_isCloseConfirmed || ShellPage == null || !ShellPage.HasCurrentPageUnsavedChanges)
            {
                return;
            }

            args.Cancel = true;
            if (await ShellPage.ConfirmCurrentPageLeaveAsync())
            {
                _isCloseConfirmed = true;
                Close();
            }
        }
    }
}
