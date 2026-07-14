using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using NekoSubscription.Core.Configuration;

namespace NekoSubscription.Core.Diagnostics;

public sealed class CrashReportService : ICrashReportService
{
    private readonly IApplicationLogger _logger;
    private readonly ApplicationStoragePaths _paths;

    public CrashReportService(IApplicationStoragePathsProvider pathsProvider, IApplicationLogger logger)
    {
        ArgumentNullException.ThrowIfNull(pathsProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _paths = pathsProvider.GetPaths();
    }

    public string? Report(Exception exception, CrashSource source)
    {
        ArgumentNullException.ThrowIfNull(exception);

        try
        {
            Directory.CreateDirectory(_paths.CrashReportsDirectory);

            var reportPath = Path.Combine(
                _paths.CrashReportsDirectory,
                $"crash-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.log");

            File.WriteAllText(reportPath, CreateReport(exception, source), Encoding.UTF8);
            _logger.Write(ApplicationLogLevel.Fatal, $"Unhandled exception reported from {source}.", exception);

            return reportPath;
        }
        catch
        {
            return null;
        }
    }

    private static string CreateReport(Exception exception, CrashSource source)
    {
        var entryAssembly = Assembly.GetEntryAssembly()?.GetName();
        var report = new StringBuilder();

        report.AppendLine("NekoSubscription crash report");
        report.AppendLine($"Timestamp (UTC): {DateTimeOffset.UtcNow:O}");
        report.AppendLine($"Source: {source}");
        report.AppendLine($"Application version: {entryAssembly?.Version?.ToString() ?? "Unknown"}");
        report.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        report.AppendLine($"Operating system: {RuntimeInformation.OSDescription}");
        report.AppendLine();
        report.AppendLine(exception.ToString());

        return report.ToString();
    }
}
