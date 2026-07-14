using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace NekoSubscription.Core.Configuration;

public sealed class SettingsStorageService : ISettingsStorageService, IDisposable
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly ApplicationStoragePaths _paths;
    private readonly DbContextOptions<ConfigurationDbContext> _options;
    private bool _isInitialized;

    public SettingsStorageService(IApplicationStoragePathsProvider pathsProvider)
    {
        ArgumentNullException.ThrowIfNull(pathsProvider);

        _paths = pathsProvider.GetPaths();
        _options = ConfigurationDbContextOptions.Create(_paths.ConfigurationDatabasePath);
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var context = CreateContext();
        var entry = await context.Settings
            .AsNoTracking()
            .SingleOrDefaultAsync(setting => setting.Key == key, cancellationToken)
            .ConfigureAwait(false);

        return entry is null ? default : JsonSerializer.Deserialize<T>(entry.Value);
    }

    public async Task<T> GetOrDefaultAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        var value = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        return value is null ? defaultValue : value;
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        var serializedValue = JsonSerializer.Serialize(value);

        await using var context = CreateContext();
        var entry = await context.Settings
            .SingleOrDefaultAsync(setting => setting.Key == key, cancellationToken)
            .ConfigureAwait(false);

        if (entry is null)
        {
            context.Settings.Add(new SettingEntry
            {
                Key = key,
                Value = serializedValue,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
        }
        else
        {
            entry.Value = serializedValue;
            entry.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var context = CreateContext();
        var deletedCount = await context.Settings
            .Where(setting => setting.Key == key)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return deletedCount > 0;
    }

    public void Dispose()
    {
        _initializationLock.Dispose();
    }

    private ConfigurationDbContext CreateContext() => new(_options);

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_isInitialized)
            {
                return;
            }

            Directory.CreateDirectory(_paths.ApplicationDataDirectory);

            await using var context = CreateContext();
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (key.Length > SettingEntry.MaximumKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(key),
                key.Length,
                $"The setting key cannot exceed {SettingEntry.MaximumKeyLength} characters.");
        }
    }
}
