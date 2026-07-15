using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using NekoSubscription.Core.Configuration;

namespace NekoSubscription.Core.Tests.Configuration;

public sealed class ApplicationSettingsServiceTests : IDisposable
{
    private const string ApplicationDirectoryName = "NekoSubscription";
    private const string ConfigurationDatabaseFileName = "configuration.db";
    private const string InitialMigrationName = "20260715010029_InitialApplicationSettings";

    private readonly string _dataRootDirectory;
    private readonly ApplicationStoragePaths _paths;

    public ApplicationSettingsServiceTests()
    {
        _dataRootDirectory = Path.Combine(
            Path.GetTempPath(),
            "NekoSubscription.Core.Tests",
            Guid.NewGuid().ToString("N"));
        var applicationDataDirectory = Path.Combine(_dataRootDirectory, ApplicationDirectoryName);
        _paths = new ApplicationStoragePaths(
            _dataRootDirectory,
            applicationDataDirectory,
            Path.Combine(applicationDataDirectory, ConfigurationDatabaseFileName),
            Path.Combine(applicationDataDirectory, "data.db"),
            Path.Combine(applicationDataDirectory, "logs"),
            Path.Combine(applicationDataDirectory, "crash-reports"));
    }

    [Fact]
    public async Task SaveAsync_RoundTripsVisualStyleAndAcrylicOpacity()
    {
        using var service = new ApplicationSettingsService(_paths);
        var settings = new ApplicationSettings
        {
            VisualStyle = ApplicationVisualStyle.Acrylic,
            AcrylicOpacity = 0.64
        };

        await service.SaveAsync(settings);
        var persistedSettings = await service.GetAsync();

        Assert.Equal(ApplicationVisualStyle.Acrylic, persistedSettings.VisualStyle);
        Assert.Equal(0.64, persistedSettings.AcrylicOpacity);
    }

    [Theory]
    [InlineData(0.19)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public async Task SaveAsync_RejectsInvalidAcrylicOpacity(double acrylicOpacity)
    {
        using var service = new ApplicationSettingsService(_paths);
        var settings = new ApplicationSettings
        {
            AcrylicOpacity = acrylicOpacity
        };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.SaveAsync(settings));

        Assert.Equal("settings", exception.ParamName);
    }

    [Fact]
    public async Task Migration_UpgradesExistingSettingsWithStandardVisualDefaults()
    {
        Directory.CreateDirectory(_paths.ApplicationDataDirectory);
        var options = ConfigurationDbContextOptions.Create(_paths.ConfigurationDatabasePath);
        await using (var context = new ConfigurationDbContext(options))
        {
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(InitialMigrationName);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO ApplicationSettings
                (Id, Theme, CultureName, MinimumLogLevel, UpdatedAtUtc)
                VALUES (1, 0, NULL, 2, '2026-07-15T00:00:00+00:00')
                """);
        }

        using var service = new ApplicationSettingsService(_paths);
        var settings = await service.GetAsync();

        Assert.Equal(ApplicationVisualStyle.Standard, settings.VisualStyle);
        Assert.Equal(ApplicationSettings.DefaultAcrylicOpacity, settings.AcrylicOpacity);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dataRootDirectory))
        {
            Directory.Delete(_dataRootDirectory, true);
        }
    }
}
