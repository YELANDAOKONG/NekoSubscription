using System;

using Avalonia.Logging;

using Serilog;

using AvaloniaLogEventLevel = Avalonia.Logging.LogEventLevel;
using SerilogLogEventLevel = Serilog.Events.LogEventLevel;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.Diagnostics;

namespace NekoSubscription;

internal sealed class AvaloniaSerilogSink : ILogSink
{
    private readonly ApplicationLogging _logging;

    public AvaloniaSerilogSink(ApplicationLogging logging)
    {
        ArgumentNullException.ThrowIfNull(logging);
        _logging = logging;
    }

    public bool IsEnabled(AvaloniaLogEventLevel level, string area)
    {
        var minimumLevel = _logging.MinimumLevel;
        return minimumLevel != ApplicationLogLevel.None
            && MapApplicationLevel(level) >= minimumLevel;
    }

    public void Log(
        AvaloniaLogEventLevel level,
        string area,
        object? source,
        string messageTemplate)
        => CreateContext(area, source).Write(MapSerilogLevel(level), messageTemplate);

    public void Log(
        AvaloniaLogEventLevel level,
        string area,
        object? source,
        string messageTemplate,
        params object?[] propertyValues)
        => CreateContext(area, source).Write(MapSerilogLevel(level), messageTemplate, propertyValues);

    private static ApplicationLogLevel MapApplicationLevel(AvaloniaLogEventLevel level)
        => level switch
        {
            AvaloniaLogEventLevel.Verbose => ApplicationLogLevel.Trace,
            AvaloniaLogEventLevel.Debug => ApplicationLogLevel.Debug,
            AvaloniaLogEventLevel.Information => ApplicationLogLevel.Information,
            AvaloniaLogEventLevel.Warning => ApplicationLogLevel.Warning,
            AvaloniaLogEventLevel.Error => ApplicationLogLevel.Error,
            AvaloniaLogEventLevel.Fatal => ApplicationLogLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "The Avalonia log level is invalid.")
        };

    private static SerilogLogEventLevel MapSerilogLevel(AvaloniaLogEventLevel level)
        => level switch
        {
            AvaloniaLogEventLevel.Verbose => SerilogLogEventLevel.Verbose,
            AvaloniaLogEventLevel.Debug => SerilogLogEventLevel.Debug,
            AvaloniaLogEventLevel.Information => SerilogLogEventLevel.Information,
            AvaloniaLogEventLevel.Warning => SerilogLogEventLevel.Warning,
            AvaloniaLogEventLevel.Error => SerilogLogEventLevel.Error,
            AvaloniaLogEventLevel.Fatal => SerilogLogEventLevel.Fatal,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "The Avalonia log level is invalid.")
        };

    private ILogger CreateContext(string area, object? source)
        => _logging.Logger
            .ForContext("AvaloniaArea", area)
            .ForContext("AvaloniaSource", source?.GetType().FullName ?? "Unknown");
}
