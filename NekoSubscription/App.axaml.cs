using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

using NekoSubscription.Core.Diagnostics;
using NekoSubscription.ViewModels;
using NekoSubscription.Views;

namespace NekoSubscription;

public partial class App : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Default;
        DataTemplates.Add(new ViewLocator());
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (_, eventArgs) =>
        {
            Program.CrashReports?.Report(eventArgs.Exception, CrashSource.AvaloniaDispatcher);
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        _ = InitializeLoggingAsync();
        base.OnFrameworkInitializationCompleted();
    }

    private static async Task InitializeLoggingAsync()
    {
        if (Program.Logger is null)
        {
            return;
        }

        try
        {
            await Program.Logger.InitializeAsync();
            Program.Logger.Write(ApplicationLogLevel.Information, "Application startup completed.");
        }
        catch (Exception exception)
        {
            Program.CrashReports?.Report(exception, CrashSource.Startup);
        }
    }
}
