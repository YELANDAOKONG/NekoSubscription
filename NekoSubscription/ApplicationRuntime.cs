using System;

using Serilog;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.DataManagement;
using NekoSubscription.Core.Diagnostics;
using NekoSubscription.Core.Subscriptions;

namespace NekoSubscription;

internal sealed class ApplicationRuntime : IDisposable
{
    private readonly AvaloniaSerilogSink _avaloniaLogSink;
    private readonly ApplicationDiagnostics _diagnostics;
    private bool _isDisposed;

    public ApplicationRuntime()
    {
        var pathsProvider = new ApplicationStoragePathsProvider();
        var paths = pathsProvider.GetPaths();

        Settings = new ApplicationSettingsService(pathsProvider);
        Subscriptions = new SubscriptionService(paths);
        DataManagement = new DataManagementService(paths);
        Logging = new ApplicationLogging(paths.LogsDirectory);
        CrashReports = new CrashReportService(
            paths.CrashReportsDirectory,
            Logging.Logger,
            Logging.LatestLogPath);
        _avaloniaLogSink = new AvaloniaSerilogSink(Logging);
        _diagnostics = new ApplicationDiagnostics(CrashReports);
    }

    public IApplicationSettingsService Settings { get; }

    public ISubscriptionService Subscriptions { get; }

    public IDataManagementService DataManagement { get; }

    public ApplicationLogging Logging { get; }

    public ILogger Logger => Logging.Logger;

    public ICrashReportService CrashReports { get; }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _diagnostics.Start();
        Logger.Information("Application runtime started.");
    }

    public void StartAvaloniaDiagnostics()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        Avalonia.Logging.Logger.Sink = _avaloniaLogSink;
        _diagnostics.StartUiDiagnostics();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Logger.Information("Application runtime stopped.");
        if (ReferenceEquals(Avalonia.Logging.Logger.Sink, _avaloniaLogSink))
        {
            Avalonia.Logging.Logger.Sink = null;
        }

        _diagnostics.Dispose();
        DataManagement.Dispose();
        Subscriptions.Dispose();
        Settings.Dispose();
        Logging.Dispose();
        _isDisposed = true;
    }
}
