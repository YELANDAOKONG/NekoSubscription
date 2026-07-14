using System;
using System.Threading.Tasks;

using Avalonia;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.Diagnostics;

namespace NekoSubscription;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var pathsProvider = new ApplicationStoragePathsProvider();
        using var settingsStorageService = new SettingsStorageService(pathsProvider);
        using var logger = new FileApplicationLogger(pathsProvider, settingsStorageService);
        var crashReportService = new CrashReportService(pathsProvider, logger);

        RegisterCrashHandlers(crashReportService);

        try
        {
            Logger = logger;
            CrashReports = crashReportService;
            logger.Write(ApplicationLogLevel.Information, "Application startup started.");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            crashReportService.Report(exception, CrashSource.Startup);
            throw;
        }
    }

    internal static IApplicationLogger? Logger { get; private set; }

    internal static ICrashReportService? CrashReports { get; private set; }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private static void RegisterCrashHandlers(ICrashReportService crashReportService)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception exception)
            {
                crashReportService.Report(exception, CrashSource.ApplicationDomain);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
        {
            crashReportService.Report(eventArgs.Exception, CrashSource.UnobservedTask);
        };
    }
}
