using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Serilog;
using Serilog.Events;

namespace NekoSubscription.Core.Diagnostics;

public sealed class CrashReportService : ICrashReportService
{
    private readonly object _syncRoot = new();
    private readonly string _crashReportsDirectory;
    private readonly string _latestLogPath;
    private readonly ILogger _logger;

    public CrashReportService(string crashReportsDirectory, ILogger logger, string latestLogPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(crashReportsDirectory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(latestLogPath);

        _crashReportsDirectory = Path.GetFullPath(crashReportsDirectory);
        _logger = logger;
        _latestLogPath = Path.GetFullPath(latestLogPath);
    }

    public string? TryWriteReport(Exception exception, string source, bool isTerminating)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        TryLogException(exception, source, isTerminating);

        try
        {
            lock (_syncRoot)
            {
                Directory.CreateDirectory(_crashReportsDirectory);
                var reportPath = GetAvailableReportPath();
                File.WriteAllText(reportPath, BuildReport(exception, source, isTerminating), new UTF8Encoding(false));
                return reportPath;
            }
        }
        catch (Exception reportException) when (IsRecoverableReportFailure(reportException))
        {
            Trace.WriteLine($"Failed to write a crash report: {reportException}");
            return null;
        }
    }

    private string BuildReport(Exception exception, string source, bool isTerminating)
    {
        var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
        var builder = new StringBuilder();
        builder.AppendLine("NekoSubscription crash report");
        builder.AppendLine($"Timestamp (UTC): {DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"Source: {source}");
        builder.AppendLine($"Process terminating: {isTerminating}");
        builder.AppendLine($"Application version: {assemblyVersion}");
        builder.AppendLine($"Framework: {RuntimeInformation.FrameworkDescription}");
        builder.AppendLine($"Operating system: {RuntimeInformation.OSDescription}");
        builder.AppendLine($"Process architecture: {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine($"Latest log: {_latestLogPath}");
        builder.AppendLine();
        builder.AppendLine("Exception:");
        builder.AppendLine(exception.ToString());
        return builder.ToString();
    }

    private static bool IsRecoverableReportFailure(Exception exception)
        => exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ArgumentException;

    private string GetAvailableReportPath()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss.fff'Z'", CultureInfo.InvariantCulture);
        var baseFileName = $"crash-{timestamp}";
        var reportPath = Path.Combine(_crashReportsDirectory, $"{baseFileName}.txt");
        var suffix = 1;

        while (File.Exists(reportPath))
        {
            reportPath = Path.Combine(_crashReportsDirectory, $"{baseFileName}-{suffix}.txt");
            suffix++;
        }

        return reportPath;
    }

    private void TryLogException(Exception exception, string source, bool isTerminating)
    {
        try
        {
            var terminationState = isTerminating ? "terminating" : "non-terminating";
            var level = isTerminating ? LogEventLevel.Fatal : LogEventLevel.Error;
            _logger.Write(
                level,
                exception,
                "Captured a {TerminationState} exception from {ExceptionSource}.",
                terminationState,
                source);
        }
        catch (Exception loggingException) when (IsRecoverableReportFailure(loggingException))
        {
            Trace.WriteLine($"Failed to log an exception before writing a crash report: {loggingException}");
        }
    }
}
