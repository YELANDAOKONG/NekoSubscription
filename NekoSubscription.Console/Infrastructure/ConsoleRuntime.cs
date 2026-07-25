using System;
using System.Threading;
using System.Threading.Tasks;
using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.DataManagement;
using NekoSubscription.Core.Diagnostics;
using NekoSubscription.Core.Subscriptions;

namespace NekoSubscription.Console.Infrastructure;

public sealed class ConsoleRuntime : IAsyncDisposable, IDisposable
{
    private bool _isDisposed;

    private ConsoleRuntime(
        ApplicationStoragePaths paths,
        ApplicationSettingsService settings,
        SubscriptionService subscriptions,
        DataManagementService dataManagement,
        ApplicationLogging logging,
        CrashReportService crashReports)
    {
        Paths = paths;
        Settings = settings;
        Subscriptions = subscriptions;
        DataManagement = dataManagement;
        Logging = logging;
        CrashReports = crashReports;
    }

    public ApplicationStoragePaths Paths { get; }
    public IApplicationSettingsService Settings { get; }
    public ISubscriptionService Subscriptions { get; }
    public IDataManagementService DataManagement { get; }
    public ApplicationLogging Logging { get; }
    public ICrashReportService CrashReports { get; }

    public static async Task<ConsoleRuntime> CreateAsync(string? customDataRoot = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(customDataRoot))
        {
            Environment.SetEnvironmentVariable(
                ApplicationStoragePathsProvider.DataRootEnvironmentVariableName,
                customDataRoot);
        }

        var pathsProvider = new ApplicationStoragePathsProvider();
        var paths = pathsProvider.GetPaths();

        var settings = new ApplicationSettingsService(pathsProvider);
        var subscriptions = new SubscriptionService(paths);
        var dataManagement = new DataManagementService(paths);
        var logging = new ApplicationLogging(paths.LogsDirectory);
        var crashReports = new CrashReportService(
            paths.CrashReportsDirectory,
            logging.Logger,
            logging.LatestLogPath);

        var runtime = new ConsoleRuntime(paths, settings, subscriptions, dataManagement, logging, crashReports);
        await runtime.InitializeAsync(cancellationToken);
        return runtime;
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Subscriptions.InitializeAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        DataManagement.Dispose();
        Subscriptions.Dispose();
        Settings.Dispose();
        Logging.Dispose();
        _isDisposed = true;
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        DataManagement.Dispose();
        Subscriptions.Dispose();
        Settings.Dispose();
        Logging.Dispose();
        _isDisposed = true;
    }
}
