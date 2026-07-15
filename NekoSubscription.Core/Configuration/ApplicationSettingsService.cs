using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace NekoSubscription.Core.Configuration;

public sealed class ApplicationSettingsService : IApplicationSettingsService, IDisposable
{
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly ApplicationStoragePaths _paths;
    private readonly DbContextOptions<ConfigurationDbContext> _options;
    private bool _isInitialized;

    public ApplicationSettingsService(IApplicationStoragePathsProvider pathsProvider)
    {
        ArgumentNullException.ThrowIfNull(pathsProvider);

        _paths = pathsProvider.GetPaths();
        _options = ConfigurationDbContextOptions.Create(_paths.ConfigurationDatabasePath);
    }

    public async Task<ApplicationSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var context = CreateContext();
        var settings = await context.Settings
            .AsNoTracking()
            .SingleOrDefaultAsync(
                settings => settings.Id == ApplicationSettings.SingletonId,
                cancellationToken)
            .ConfigureAwait(false);

        return settings ?? new ApplicationSettings();
    }

    public async Task SaveAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!Enum.IsDefined(settings.Theme))
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                settings.Theme,
                "The application theme is invalid.");
        }

        if (!Enum.IsDefined(settings.MinimumLogLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings),
                settings.MinimumLogLevel,
                "The minimum log level is invalid.");
        }

        settings.CultureName = NormalizeCultureName(settings.CultureName);

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var context = CreateContext();
        var persistedSettings = await context.Settings
            .SingleOrDefaultAsync(
                candidate => candidate.Id == ApplicationSettings.SingletonId,
                cancellationToken)
            .ConfigureAwait(false);

        var updatedAtUtc = DateTimeOffset.UtcNow;
        settings.MarkUpdated(updatedAtUtc);

        if (persistedSettings is null)
        {
            context.Settings.Add(settings);
        }
        else
        {
            persistedSettings.Theme = settings.Theme;
            persistedSettings.CultureName = settings.CultureName;
            persistedSettings.MinimumLogLevel = settings.MinimumLogLevel;
            persistedSettings.MarkUpdated(updatedAtUtc);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _initializationLock.Dispose();
    }

    private ConfigurationDbContext CreateContext() => new(_options);

    private static string? NormalizeCultureName(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return null;
        }

        if (cultureName.Length > ApplicationSettings.MaximumCultureNameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cultureName),
                cultureName.Length,
                $"The culture name cannot exceed {ApplicationSettings.MaximumCultureNameLength} characters.");
        }

        try
        {
            return CultureInfo.GetCultureInfo(cultureName).Name;
        }
        catch (CultureNotFoundException exception)
        {
            throw new ArgumentException("The culture name is invalid.", nameof(cultureName), exception);
        }
    }

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
}
