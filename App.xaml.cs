using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using DevDeck.Contracts;
using DevDeck.Services;
using DevDeck.Shell;
using DevDeck.Data;
using DevDeck.Features.Home;
using DevDeck.Features.Projects;
using DevDeck.Features.Actions;
using DevDeck.Features.Settings;
using DevDeck.Enums;

namespace DevDeck
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; } = null!;
        public static MainWindow MainWindow { get; private set; } = null!;

        public App()
        {
            InitializeComponent();

            // Register global exception handlers
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            UnhandledException += App_UnhandledException;

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Core services
                    services.AddSingleton<IPathResolverService, PathResolverService>();
                    services.AddSingleton<WindowHandleService>();
                    services.AddSingleton<AppStateService>();
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddSingleton<IDataService, DataService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IWorkspaceService, WorkspaceService>();
                    services.AddSingleton<IProjectService, ProjectService>();
                    services.AddSingleton<IIconStorageService, IconStorageService>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<IActionService, ActionService>();
                    services.AddSingleton<IVariableResolver, VariableResolver>();
                    services.AddTransient<ActionOverrideResolver>();
                    services.AddSingleton<IActionExecutionService, ActionExecutionService>();
                    services.AddTransient<ITerminalBackend, ConPtyTerminalBackend>();
                    services.AddSingleton<ITerminalManager, TerminalManager>();

                    // ViewModels
                    services.AddTransient<ShellViewModel>();
                    services.AddTransient<HomeViewModel>();
                    services.AddTransient<ProjectViewModel>();
                    services.AddTransient<ActionsViewModel>();
                    services.AddTransient<SettingsViewModel>();
                    
                    // Windows & Views
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<ShellPage>();
                    services.AddTransient<HomePage>();
                    services.AddTransient<ProjectPage>();
                    services.AddTransient<ActionsPage>();
                    services.AddTransient<SettingsPage>();
                })
                .Build();
        }

        private void App_UnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            LoggerHelper.LogToFile("Global_WinUI_UnhandledException", e.Exception);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
        {
            LoggerHelper.LogToFile("Global_AppDomain_UnhandledException", e.ExceptionObject as Exception);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LoggerHelper.LogToFile("Global_TaskScheduler_UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            await AppHost.StartAsync();

            // Load settings
            var settingsService = AppHost.Services.GetRequiredService<ISettingsService>();
            await settingsService.LoadAsync();

            // Load data
            var dataService = AppHost.Services.GetRequiredService<IDataService>();
            await dataService.LoadAsync();

            MainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            
            // Register window handle
            var handleService = AppHost.Services.GetRequiredService<WindowHandleService>();
            handleService.RegisterWindow(MainWindow);

            // Apply theme
            ApplyTheme(settingsService.Settings.Theme);
            ApplyBackdrop(settingsService.Settings.Backdrop);

            // Navigate MainWindow to ShellPage
            var shellPage = AppHost.Services.GetRequiredService<ShellPage>();
            MainWindow.NavigateToShellPage(shellPage);

            MainWindow.Activate();
        }

        public static void ApplyTheme(AppTheme theme)
        {
            if (MainWindow?.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme switch
                {
                    AppTheme.Light => ElementTheme.Light,
                    AppTheme.Dark => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }

        public static void ApplyBackdrop(BackdropKind backdrop)
        {
            MainWindow?.ApplyBackdrop(backdrop);
        }
    }
}
