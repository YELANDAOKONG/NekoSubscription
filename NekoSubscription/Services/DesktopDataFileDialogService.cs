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
    private const string CsvExtension = ".csv";
    private const string BackupExtension = ".nekobackup";

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
                    AppleUniformTypeIdentifiers = ["public.comma-separated-values-text"],
                    MimeTypes = ["text/csv"],
                    Patterns = [$"*{CsvExtension}"]
                }
            ],
            Title = title
        });
        cancellationToken.ThrowIfCancellationRequested();
        if (files.Count == 0)
        {
            return null;
        }

        var file = files[0];
        if (!string.Equals(
                Path.GetExtension(file.Name),
                CsvExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The selected file must have a .csv extension.");
        }

        return await file.OpenReadAsync();
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
                    AppleUniformTypeIdentifiers = ["public.data"],
                    MimeTypes = ["application/zip"],
                    Patterns = [$"*{BackupExtension}"]
                }
            ],
            SuggestedFileName =
                $"NekoSubscription-{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}{BackupExtension}",
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
