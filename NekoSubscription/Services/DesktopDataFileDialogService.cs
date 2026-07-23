using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace NekoSubscription.Services;

public sealed class DesktopDataFileDialogService : IDataFileDialogService
{
    public async Task<Stream?> OpenCsvFileAsync(
        string title,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        cancellationToken.ThrowIfCancellationRequested();
        var storageProvider = GetStorageProvider();
        if (!storageProvider.CanOpen)
        {
            throw new NotSupportedException("Opening files is not supported on this platform.");
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("CSV")
                {
                    MimeTypes = ["text/csv", "text/plain"],
                    Patterns = ["*.csv"]
                }
            ],
            Title = title
        });
        cancellationToken.ThrowIfCancellationRequested();
        return files.Count == 0
            ? null
            : await files[0].OpenReadAsync();
    }

    public async Task<Stream?> CreateBackupFileAsync(
        string title,
        string fileTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileTypeName);
        cancellationToken.ThrowIfCancellationRequested();
        var storageProvider = GetStorageProvider();
        if (!storageProvider.CanSave)
        {
            throw new NotSupportedException("Saving files is not supported on this platform.");
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            DefaultExtension = "nekobackup",
            FileTypeChoices =
            [
                new FilePickerFileType(fileTypeName)
                {
                    MimeTypes = ["application/zip"],
                    Patterns = ["*.nekobackup"]
                }
            ],
            SuggestedFileName = $"NekoSubscription-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}",
            Title = title
        });
        cancellationToken.ThrowIfCancellationRequested();
        return file is null
            ? null
            : await file.OpenWriteAsync();
    }

    private static IStorageProvider GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is
                IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow })
        {
            return mainWindow.StorageProvider;
        }

        throw new InvalidOperationException("The main application window is unavailable.");
    }
}
