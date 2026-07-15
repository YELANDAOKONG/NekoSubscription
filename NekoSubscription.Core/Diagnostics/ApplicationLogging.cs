using System;

using Serilog;
using Serilog.Core;
using Serilog.Formatting.Display;

using NekoSubscription.Core.Configuration;

namespace NekoSubscription.Core.Diagnostics;

public sealed class ApplicationLogging : IDisposable
{
    public const long DefaultMaximumLogFileSizeBytes = 50 * 1024 * 1024;

    private const string OutputTemplate =
        "[{Timestamp:O}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private readonly Logger _logger;
    private readonly FixedFileLogSink _sink;

    public ApplicationLogging(
        string logsDirectory,
        ApplicationLogLevel minimumLevel = ApplicationLogLevel.Information,
        long maximumLogFileSizeBytes = DefaultMaximumLogFileSizeBytes)
    {
        var formatter = new MessageTemplateTextFormatter(OutputTemplate, null);
        _sink = new FixedFileLogSink(
            logsDirectory,
            formatter,
            minimumLevel,
            maximumLogFileSizeBytes);
        _logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    public ILogger Logger => _logger;

    public string LatestLogPath => _sink.LatestLogPath;

    public ApplicationLogLevel MinimumLevel
    {
        get => _sink.MinimumLevel;
        set => _sink.MinimumLevel = value;
    }

    public void Dispose() => _logger.Dispose();
}
