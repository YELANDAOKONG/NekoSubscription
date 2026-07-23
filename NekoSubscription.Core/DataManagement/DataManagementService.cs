using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.Subscriptions;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.DataManagement;

public sealed class DataManagementService : IDataManagementService
{
    private const int StreamBufferSize = 81920;

    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly ApplicationStoragePaths _paths;
    private bool _isDisposed;

    public DataManagementService(IApplicationStoragePathsProvider pathsProvider)
    {
        ArgumentNullException.ThrowIfNull(pathsProvider);
        _paths = pathsProvider.GetPaths();
    }

    public DataManagementService(ApplicationStoragePaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths = paths;
    }

    public async Task CreateBackupAsync(
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The backup destination must be writable.", nameof(destination));
        }

        await ExecuteExclusiveAsync(
                async operationCancellationToken =>
                {
                    await EnsureDatabasesInitializedAsync(operationCancellationToken).ConfigureAwait(false);

                    var temporaryDirectory = Directory.CreateTempSubdirectory("NekoSubscriptionBackup-");
                    try
                    {
                        var dataSnapshotPath = Path.Combine(temporaryDirectory.FullName, "data.db");
                        var configurationSnapshotPath = Path.Combine(
                            temporaryDirectory.FullName,
                            "configuration.db");
                        CreateDatabaseSnapshot(_paths.DataDatabasePath, dataSnapshotPath);
                        CreateDatabaseSnapshot(
                            _paths.ConfigurationDatabasePath,
                            configurationSnapshotPath);

                        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
                        await AddFileToArchiveAsync(
                                archive,
                                dataSnapshotPath,
                                "data.db",
                                operationCancellationToken)
                            .ConfigureAwait(false);
                        await AddFileToArchiveAsync(
                                archive,
                                configurationSnapshotPath,
                                "configuration.db",
                                operationCancellationToken)
                            .ConfigureAwait(false);
                        await AddManifestAsync(archive, operationCancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        temporaryDirectory.Delete(recursive: true);
                    }

                    return true;
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<CsvImportPreview> PreviewSubscriptionCsvAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        var csvData = await ReadCsvDataAsync(source, cancellationToken).ConfigureAwait(false);
        return StandardSubscriptionCsvParser.Parse(csvData).Preview;
    }

    public async Task<CsvImportResult> ImportSubscriptionCsvAsync(
        Stream source,
        CancellationToken cancellationToken = default)
    {
        var csvData = await ReadCsvDataAsync(source, cancellationToken).ConfigureAwait(false);
        var parsedCsv = StandardSubscriptionCsvParser.Parse(csvData);
        if (!parsedCsv.Preview.CanImport)
        {
            throw new InvalidDataException("The CSV file contains errors and cannot be imported.");
        }

        return await ExecuteExclusiveAsync(
                async operationCancellationToken =>
                {
                    await EnsureDatabasesInitializedAsync(operationCancellationToken).ConfigureAwait(false);

                    var options = SubscriptionDbContextOptions.Create(_paths.DataDatabasePath);
                    await using var context = new SubscriptionDbContext(options);
                    await using var transaction = await context.Database
                        .BeginTransactionAsync(operationCancellationToken)
                        .ConfigureAwait(false);

                    var paymentProfiles = await context.PaymentProfiles
                        .ToListAsync(operationCancellationToken)
                        .ConfigureAwait(false);
                    var paymentProfilesByKey = paymentProfiles
                        .GroupBy(CreatePaymentProfileKey)
                        .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                    var createdPaymentProfileCount = 0;
                    var changedAtUtc = DateTimeOffset.UtcNow;

                    foreach (var importedRow in parsedCsv.Rows)
                    {
                        var subscription = CreateSubscription(importedRow, changedAtUtc);
                        var paymentProfile = GetOrCreatePaymentProfile(
                            context,
                            paymentProfilesByKey,
                            importedRow,
                            changedAtUtc,
                            ref createdPaymentProfileCount);
                        if (paymentProfile is not null)
                        {
                            subscription.SetPaymentProfile(paymentProfile, changedAtUtc);
                        }

                        context.Subscriptions.Add(subscription);
                    }

                    await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(operationCancellationToken).ConfigureAwait(false);

                    return new CsvImportResult(parsedCsv.Rows.Count, createdPaymentProfileCount);
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<DataClearResult> ClearSubscriptionDataAsync(
        CancellationToken cancellationToken = default)
    {
        return await ExecuteExclusiveAsync(
                async operationCancellationToken =>
                {
                    await EnsureDatabasesInitializedAsync(operationCancellationToken).ConfigureAwait(false);

                    var options = SubscriptionDbContextOptions.Create(_paths.DataDatabasePath);
                    await using var context = new SubscriptionDbContext(options);
                    var subscriptionCount = await context.Subscriptions
                        .IgnoreQueryFilters()
                        .CountAsync(operationCancellationToken)
                        .ConfigureAwait(false);
                    var paymentProfileCount = await context.PaymentProfiles
                        .CountAsync(operationCancellationToken)
                        .ConfigureAwait(false);
                    var tagCount = await context.Tags
                        .CountAsync(operationCancellationToken)
                        .ConfigureAwait(false);

                    await using var transaction = await context.Database
                        .BeginTransactionAsync(operationCancellationToken)
                        .ConfigureAwait(false);
                    await context.Database.ExecuteSqlRawAsync(
                            "UPDATE Subscriptions SET IncludedWithSubscriptionId = NULL, PaymentProfileId = NULL;",
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    await context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM SubscriptionTags;",
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    await context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM CustomFields;",
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    await DeleteSubscriptionSubtypeRowsAsync(context, operationCancellationToken)
                        .ConfigureAwait(false);
                    await context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM Subscriptions;",
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    await context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM PaymentProfiles;",
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    await context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM Tags;",
                            operationCancellationToken)
                        .ConfigureAwait(false);
                    await transaction.CommitAsync(operationCancellationToken).ConfigureAwait(false);

                    return new DataClearResult(subscriptionCount, paymentProfileCount, tagCount);
                },
                cancellationToken)
            .ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _operationLock.Dispose();
        _isDisposed = true;
    }

    private static OrdinarySubscription CreateSubscription(
        ImportedSubscriptionRow importedRow,
        DateTimeOffset changedAtUtc)
    {
        var billingSchedule = new BillingSchedule(
            BillingCadence.Recurring,
            importedRow.IntervalUnit,
            importedRow.IntervalCount,
            importedRow.StartsOn,
            importedRow.NextBillingOn,
            null,
            importedRow.IsActive);
        var subscription = new OrdinarySubscription(
            importedRow.ProviderName,
            importedRow.ServiceName,
            null,
            importedRow.AccountName,
            importedRow.BillingAmount,
            billingSchedule);
        subscription.SetStatuses(
            importedRow.IsActive
                ? SubscriptionConfirmationStatus.ConfirmedActive
                : SubscriptionConfirmationStatus.ConfirmedInactive,
            importedRow.IsActive
                ? SubscriptionLifecycleStatus.Active
                : SubscriptionLifecycleStatus.Cancelled,
            changedAtUtc);
        subscription.UpdateNotesAndManagementUrl(importedRow.Notes, null, changedAtUtc);
        return subscription;
    }

    private static PaymentProfile? GetOrCreatePaymentProfile(
        SubscriptionDbContext context,
        IDictionary<string, PaymentProfile> paymentProfilesByKey,
        ImportedSubscriptionRow importedRow,
        DateTimeOffset changedAtUtc,
        ref int createdPaymentProfileCount)
    {
        if (importedRow.PaymentAccount is null)
        {
            return null;
        }

        var paymentProfileKey = CreatePaymentProfileKey(
            importedRow.PaymentChannel,
            importedRow.PaymentAccount);
        if (paymentProfilesByKey.TryGetValue(paymentProfileKey, out var paymentProfile))
        {
            if (paymentProfile.IsArchived)
            {
                paymentProfile.RestoreFromArchive(changedAtUtc);
            }

            return paymentProfile;
        }

        var displayName = $"{FormatPaymentChannel(importedRow.PaymentChannel)} · {importedRow.PaymentAccount}";
        if (displayName.Length > PaymentProfile.MaximumDisplayNameLength)
        {
            displayName = displayName[..PaymentProfile.MaximumDisplayNameLength];
        }

        paymentProfile = new PaymentProfile(
            displayName,
            importedRow.PaymentChannel,
            importedRow.PaymentAccount,
            FormatPaymentChannel(importedRow.PaymentChannel),
            null);
        context.PaymentProfiles.Add(paymentProfile);
        paymentProfilesByKey.Add(paymentProfileKey, paymentProfile);
        createdPaymentProfileCount++;
        return paymentProfile;
    }

    private static string CreatePaymentProfileKey(PaymentProfile paymentProfile) =>
        CreatePaymentProfileKey(paymentProfile.Channel, paymentProfile.AccountIdentifier);

    private static string CreatePaymentProfileKey(PaymentChannel channel, string? accountIdentifier) =>
        $"{(int)channel}\u001F{accountIdentifier?.Trim().ToUpperInvariant()}";

    private static string FormatPaymentChannel(PaymentChannel channel) => channel switch
    {
        PaymentChannel.Direct => "Direct",
        PaymentChannel.AppleAppStore => "Apple App Store",
        PaymentChannel.GooglePlay => "Google Play",
        PaymentChannel.PayPal => "PayPal",
        PaymentChannel.BankTransfer => "Bank transfer",
        PaymentChannel.CreditCard => "Credit card",
        PaymentChannel.DebitCard => "Debit card",
        PaymentChannel.Cash => "Cash",
        PaymentChannel.Other => "Other",
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "The payment channel is invalid.")
    };

    private static async Task DeleteSubscriptionSubtypeRowsAsync(
        SubscriptionDbContext context,
        CancellationToken cancellationToken)
    {
        string[] deleteCommands =
        [
            "DELETE FROM OrdinarySubscriptions;",
            "DELETE FROM PhoneNumberSubscriptions;",
            "DELETE FROM DomainSubscriptions;",
            "DELETE FROM CloudServiceSubscriptions;",
            "DELETE FROM CustomSubscriptions;"
        ];

        foreach (var deleteCommand in deleteCommands)
        {
            await context.Database.ExecuteSqlRawAsync(
                    deleteCommand,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static void CreateDatabaseSnapshot(string sourcePath, string destinationPath)
    {
        var sourceConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();
        var destinationConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        using var sourceConnection = new SqliteConnection(sourceConnectionString);
        using var destinationConnection = new SqliteConnection(destinationConnectionString);
        sourceConnection.Open();
        destinationConnection.Open();
        sourceConnection.BackupDatabase(destinationConnection);
    }

    private static async Task AddFileToArchiveAsync(
        ZipArchive archive,
        string sourcePath,
        string entryName,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        await using var source = File.OpenRead(sourcePath);
        await using var destination = entry.Open();
        await source.CopyToAsync(destination, StreamBufferSize, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AddManifestAsync(
        ZipArchive archive,
        CancellationToken cancellationToken)
    {
        var manifest = new
        {
            Format = "NekoSubscriptionBackup",
            Version = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Files = new[] { "data.db", "configuration.db" }
        };
        var entry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
        await using var destination = entry.Open();
        await JsonSerializer.SerializeAsync(destination, manifest, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsureDatabasesInitializedAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.ApplicationDataDirectory);

        var subscriptionOptions = SubscriptionDbContextOptions.Create(_paths.DataDatabasePath);
        await using (var subscriptionContext = new SubscriptionDbContext(subscriptionOptions))
        {
            await subscriptionContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        var configurationOptions = ConfigurationDbContextOptions.Create(_paths.ConfigurationDatabasePath);
        await using var configurationContext = new ConfigurationDbContext(configurationOptions);
        await configurationContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ReadOnlyMemory<byte>> ReadCsvDataAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("The CSV source must be readable.", nameof(source));
        }

        using var buffer = new MemoryStream();
        var copyBuffer = new byte[StreamBufferSize];
        int bytesRead;

        while ((bytesRead = await source
                   .ReadAsync(copyBuffer.AsMemory(0, copyBuffer.Length), cancellationToken)
                   .ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + bytesRead > StandardSubscriptionCsvParser.MaximumFileSize)
            {
                throw new InvalidDataException(
                    $"The CSV file cannot exceed {StandardSubscriptionCsvParser.MaximumFileSize.ToString(CultureInfo.InvariantCulture)} bytes.");
            }

            await buffer
                .WriteAsync(copyBuffer.AsMemory(0, bytesRead), cancellationToken)
                .ConfigureAwait(false);
        }

        return buffer.ToArray();
    }

    private async Task<T> ExecuteExclusiveAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operationLock.Release();
        }
    }
}
