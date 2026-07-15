using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

using NekoSubscription.Core.Configuration;

namespace NekoSubscription.Core.Diagnostics;

internal sealed class FixedFileLogSink : ILogEventSink, IDisposable
{
    private const int WriterBufferSize = 4096;
    private const string LatestLogFileName = "latest.log";

    private readonly ITextFormatter _formatter;
    private readonly string _logsDirectory;
    private readonly long _maximumLogFileSizeBytes;
    private readonly object _syncRoot = new();
    private FileStream? _stream;
    private StreamWriter? _writer;
    private int _minimumLevel;
    private bool _isDisposed;

    public FixedFileLogSink(
        string logsDirectory,
        ITextFormatter formatter,
        ApplicationLogLevel minimumLevel,
        long maximumLogFileSizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logsDirectory);
        ArgumentNullException.ThrowIfNull(formatter);

        if (maximumLogFileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLogFileSizeBytes),
                maximumLogFileSizeBytes,
                "The maximum log file size must be greater than zero.");
        }

        ValidateMinimumLevel(minimumLevel);

        _formatter = formatter;
        _logsDirectory = Path.GetFullPath(logsDirectory);
        _maximumLogFileSizeBytes = maximumLogFileSizeBytes;
        _minimumLevel = (int)minimumLevel;
        LatestLogPath = Path.Combine(_logsDirectory, LatestLogFileName);

        Directory.CreateDirectory(_logsDirectory);
        ArchiveLatestLogIfPresent();
        OpenLatestLog();
    }

    public string LatestLogPath { get; }

    public ApplicationLogLevel MinimumLevel
    {
        get => (ApplicationLogLevel)Volatile.Read(ref _minimumLevel);
        set
        {
            ValidateMinimumLevel(value);
            Volatile.Write(ref _minimumLevel, (int)value);
        }
    }

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        if (!IsEnabled(logEvent.Level))
        {
            return;
        }

        lock (_syncRoot)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            using var buffer = new StringWriter(CultureInfo.InvariantCulture);
            _formatter.Format(logEvent, buffer);
            var renderedEvent = buffer.ToString();
            var eventSize = Encoding.UTF8.GetByteCount(renderedEvent);

            if (_stream!.Length > 0 && _stream.Length + eventSize > _maximumLogFileSizeBytes)
            {
                RotateLatestLog();
            }

            _writer!.Write(renderedEvent);
        }
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            if (_isDisposed)
            {
                return;
            }

            _writer?.Dispose();
            _stream?.Dispose();
            _writer = null;
            _stream = null;
            _isDisposed = true;
        }
    }

    private static ApplicationLogLevel MapLevel(LogEventLevel level)
        => level switch
        {
            LogEventLevel.Verbose => ApplicationLogLevel.Trace,
            LogEventLevel.Debug => ApplicationLogLevel.Debug,
            LogEventLevel.Information => ApplicationLogLevel.Information,
            LogEventLevel.Warning => ApplicationLogLevel.Warning,
            LogEventLevel.Error => ApplicationLogLevel.Error,
            LogEventLevel.Fatal => ApplicationLogLevel.Critical,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "The Serilog event level is invalid.")
        };

    private static void TryDeleteTemporaryArchive(string temporaryArchivePath)
    {
        try
        {
            File.Delete(temporaryArchivePath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void ValidateMinimumLevel(ApplicationLogLevel level)
    {
        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The minimum log level is invalid.");
        }
    }

    private bool IsEnabled(LogEventLevel level)
    {
        var minimumLevel = MinimumLevel;
        return minimumLevel != ApplicationLogLevel.None && MapLevel(level) >= minimumLevel;
    }

    private void ArchiveLatestLogIfPresent()
    {
        if (!File.Exists(LatestLogPath))
        {
            return;
        }

        if (new FileInfo(LatestLogPath).Length == 0)
        {
            File.Delete(LatestLogPath);
            return;
        }

        CompressLatestLog();
    }

    private void RotateLatestLog()
    {
        _writer!.Dispose();
        _stream!.Dispose();
        _writer = null;
        _stream = null;

        CompressLatestLog();
        OpenLatestLog();
    }

    private void CompressLatestLog()
    {
        var archivePath = GetAvailableArchivePath();
        var temporaryArchivePath = $"{archivePath}.tmp";
        var archiveCompleted = false;

        try
        {
            using (var source = new FileStream(
                       LatestLogPath,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.Read))
            using (var destination = new FileStream(
                       temporaryArchivePath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            using (var compressionStream = new GZipStream(destination, CompressionLevel.SmallestSize))
            {
                source.CopyTo(compressionStream);
            }

            archiveCompleted = true;
        }
        finally
        {
            if (!archiveCompleted)
            {
                TryDeleteTemporaryArchive(temporaryArchivePath);
            }
        }

        File.Move(temporaryArchivePath, archivePath);
        File.Delete(LatestLogPath);
    }

    private string GetAvailableArchivePath()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd'T'HHmmss.fff'Z'", CultureInfo.InvariantCulture);
        var baseFileName = $"log-{timestamp}";
        var archivePath = Path.Combine(_logsDirectory, $"{baseFileName}.log.gz");
        var suffix = 1;

        while (File.Exists(archivePath) || File.Exists($"{archivePath}.tmp"))
        {
            archivePath = Path.Combine(_logsDirectory, $"{baseFileName}-{suffix}.log.gz");
            suffix++;
        }

        return archivePath;
    }

    private void OpenLatestLog()
    {
        _stream = new FileStream(
            LatestLogPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.Read,
            WriterBufferSize,
            FileOptions.SequentialScan);
        _writer = new StreamWriter(_stream, new UTF8Encoding(false), WriterBufferSize, true)
        {
            AutoFlush = true
        };
    }
}
