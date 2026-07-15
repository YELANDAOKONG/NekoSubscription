using System;
using System.Threading.Tasks;

using Avalonia.Threading;

using NekoSubscription.Core.Diagnostics;

namespace NekoSubscription;

internal sealed class ApplicationDiagnostics : IDisposable
{
    private readonly ICrashReportService _crashReportService;
    private bool _isStarted;
    private bool _isUiDiagnosticsStarted;

    public ApplicationDiagnostics(ICrashReportService crashReportService)
    {
        ArgumentNullException.ThrowIfNull(crashReportService);
        _crashReportService = crashReportService;
    }

    public void Start()
    {
        if (_isStarted)
        {
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        _isStarted = true;
    }

    public void StartUiDiagnostics()
    {
        if (_isUiDiagnosticsStarted)
        {
            return;
        }

        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
        _isUiDiagnosticsStarted = true;
    }

    public void Dispose()
    {
        if (!_isStarted && !_isUiDiagnosticsStarted)
        {
            return;
        }

        if (_isUiDiagnosticsStarted)
        {
            Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;
            _isUiDiagnosticsStarted = false;
        }

        AppDomain.CurrentDomain.UnhandledException -= OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        _isStarted = false;
    }

    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs eventArgs)
    {
        _crashReportService.TryWriteReport(
            eventArgs.Exception,
            "Avalonia UI dispatcher",
            true);
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
    {
        var exception = eventArgs.ExceptionObject as Exception
            ?? new InvalidOperationException("The runtime raised a non-Exception unhandled error object.");

        _crashReportService.TryWriteReport(
            exception,
            "AppDomain",
            eventArgs.IsTerminating);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs)
    {
        _crashReportService.TryWriteReport(
            eventArgs.Exception,
            "TaskScheduler",
            false);
        eventArgs.SetObserved();
    }
}
