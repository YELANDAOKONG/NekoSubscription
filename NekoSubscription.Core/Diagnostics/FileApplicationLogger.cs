using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

using NekoSubscription.Core.Configuration;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace NekoSubscription.Core.Diagnostics;

public sealed class FileApplicationLogger : IApplicationLogger
{
    private const string MinimumLevelSettingKey = "diagnostics.minimum-log-level";
    private const string ArchiveDirectoryName = "archive";

    private readonly ISettingsStorageService _settingsStorageService;
    private readonly ApplicationStoragePaths _paths;
    private readonly LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Information);
    private readonly ILogger _logger;
    private ApplicationLogLevel _minimumLevel = ApplicationLogLevel.Information;
    private bool _isDisposed;

    public FileApplicationLogger(
        IApplicationStoragePathsProvider pathsProvider,
        ISettingsStorageService settingsStorageService)
    {
        ArgumentNullException.ThrowIfNull(pathsProvider);
        ArgumentNullException.ThrowIfNull(settingsStorageService);

        _settingsStorageService = settingsStorageService;
        _paths = pathsProvider.GetPaths();

        Directory.CreateDirectory(_paths.LogsDirectory);
        ArchivePreviousLog();

        _logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .Enrich.FromLogContext()
            .WriteTo.File(
                _paths.LatestLogPath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                shared: true)
            .CreateLogger();
    }

    public ApplicationLogLevel MinimumLevel => _minimumLevel;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var storedLevel = await _settingsStorageService
            .GetOrDefaultAsync(MinimumLevelSettingKey, ApplicationLogLevel.Information, cancellationToken)
            .ConfigureAwait(false);

        var minimumLevel = Enum.IsDefined(storedLevel)
            ? storedLevel
            : ApplicationLogLevel.Information;

        ApplyMinimumLevel(minimumLevel);
        Write(ApplicationLogLevel.Information, "Application logging initialized.");
    }

    public async Task SetMinimumLevelAsync(
        ApplicationLogLevel minimumLevel,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ValidateMinimumLevel(minimumLevel);

        await _settingsStorageService
            .SetAsync(MinimumLevelSettingKey, minimumLevel, cancellationToken)
            .ConfigureAwait(false);

        ApplyMinimumLevel(minimumLevel);
        Write(ApplicationLogLevel.Information, $"Minimum log level changed to {minimumLevel}.");
    }

    public void Write(ApplicationLogLevel level, string message, Exception? exception = null)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ValidateMinimumLevel(level);

        if (_minimumLevel is ApplicationLogLevel.None || level < _minimumLevel)
        {
            return;
        }

        _logger.Write(ToSerilogLevel(level), exception, "{Message}", message);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _logger.Dispose();
    }

    private void ApplyMinimumLevel(ApplicationLogLevel minimumLevel)
    {
        _minimumLevel = minimumLevel;
        _levelSwitch.MinimumLevel = minimumLevel is ApplicationLogLevel.None
            ? LogEventLevel.Fatal
            : ToSerilogLevel(minimumLevel);
    }

    private void ArchivePreviousLog()
    {
        if (!File.Exists(_paths.LatestLogPath))
        {
            return;
        }

        var logFile = new FileInfo(_paths.LatestLogPath);

        if (logFile.Length == 0)
        {
            return;
        }

        var archiveDirectory = Path.Combine(_paths.LogsDirectory, ArchiveDirectoryName);
        Directory.CreateDirectory(archiveDirectory);

        var archivePath = Path.Combine(
            archiveDirectory,
            $"log-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.log.gz");

        using (var source = File.OpenRead(_paths.LatestLogPath))
        using (var destination = File.Create(archivePath))
        using (var compressedDestination = new GZipStream(destination, CompressionLevel.SmallestSize))
        {
            source.CopyTo(compressedDestination);
        }

        File.Delete(_paths.LatestLogPath);
    }

    private static LogEventLevel ToSerilogLevel(ApplicationLogLevel level) => level switch
    {
        ApplicationLogLevel.Trace => LogEventLevel.Verbose,
        ApplicationLogLevel.Debug => LogEventLevel.Debug,
        ApplicationLogLevel.Information => LogEventLevel.Information,
        ApplicationLogLevel.Warning => LogEventLevel.Warning,
        ApplicationLogLevel.Error => LogEventLevel.Error,
        ApplicationLogLevel.Fatal => LogEventLevel.Fatal,
        ApplicationLogLevel.None => LogEventLevel.Fatal,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "The log level is not supported.")
    };

    private static void ValidateMinimumLevel(ApplicationLogLevel level)
    {
        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The log level is not supported.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
