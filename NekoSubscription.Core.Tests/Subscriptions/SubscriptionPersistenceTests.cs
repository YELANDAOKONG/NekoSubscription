using System;
using System.Data.Common;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NekoSubscription.Core.Subscriptions;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Tests.Subscriptions;

public sealed class SubscriptionPersistenceTests
{
    [Fact]
    public async Task SaveChangesAsync_PersistsConcreteTypesInSeparateTables()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var customSubscription = new CustomSubscription(
            "Custom provider",
            "Custom service",
            null,
            "custom@example.com",
            CreateMoney(),
            CreateSchedule());
        customSubscription.Fields.Add(CustomField.CreateText("Environment", "Production"));

        context.Subscriptions.AddRange(
            CreateOrdinarySubscription("ordinary@example.com"),
            new PhoneNumberSubscription(
                "Mobile provider",
                "Mobile service",
                "Monthly plan",
                "phone@example.com",
                CreateMoney(),
                CreateSchedule(),
                "+1 555 0100",
                PhoneNumberType.Mobile,
                "Mobile carrier",
                "United States",
                false),
            new DomainSubscription(
                "Domain registrar",
                "Domain registration",
                null,
                "domain@example.com",
                CreateMoney(),
                CreateSchedule(),
                "example.com",
                new DateOnly(2025, 1, 1),
                new DateOnly(2027, 1, 1)),
            new CloudServiceSubscription(
                "Cloud provider",
                "Compute service",
                null,
                "cloud@example.com",
                CreateMoney(),
                CreateSchedule(),
                CloudBillingMode.UsageBasedEstimate,
                "tenant-1",
                "project-1"),
            customSubscription);

        await context.SaveChangesAsync();

        Assert.Equal(5, await CountRowsAsync(connection, "Subscriptions"));
        Assert.Equal(1, await CountRowsAsync(connection, "OrdinarySubscriptions"));
        Assert.Equal(1, await CountRowsAsync(connection, "PhoneNumberSubscriptions"));
        Assert.Equal(1, await CountRowsAsync(connection, "DomainSubscriptions"));
        Assert.Equal(1, await CountRowsAsync(connection, "CloudServiceSubscriptions"));
        Assert.Equal(1, await CountRowsAsync(connection, "CustomSubscriptions"));
        Assert.Equal(1, await CountRowsAsync(connection, "CustomFields"));
    }

    [Fact]
    public async Task SaveChangesAsync_RoundTripsSharedPaymentProfileBundleAndTags()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var paymentProfile = new PaymentProfile(
            "Primary Google account",
            PaymentChannel.GooglePlay,
            "billing@example.com",
            "Google",
            null);
        var tag = new Tag("Entertainment");
        var parent = CreateOrdinarySubscription("parent@example.com");
        var child = CreateOrdinarySubscription("child@example.com");
        var changedAtUtc = DateTimeOffset.UtcNow;

        parent.SetPaymentProfile(paymentProfile, changedAtUtc);
        child.SetPaymentProfile(paymentProfile, changedAtUtc);
        child.SetIncludedWithSubscription(parent, changedAtUtc);
        parent.Tags.Add(tag);
        child.Tags.Add(tag);

        context.Subscriptions.AddRange(parent, child);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedChild = await context.OrdinarySubscriptions
            .Include(subscription => subscription.PaymentProfile)
            .Include(subscription => subscription.IncludedWithSubscription)
            .Include(subscription => subscription.Tags)
            .SingleAsync(subscription => subscription.Id == child.Id);

        Assert.Equal(paymentProfile.Id, persistedChild.PaymentProfileId);
        Assert.Equal("billing@example.com", persistedChild.PaymentProfile?.AccountIdentifier);
        Assert.Equal(parent.Id, persistedChild.IncludedWithSubscriptionId);
        Assert.Equal(parent.ServiceName, persistedChild.IncludedWithSubscription?.ServiceName);
        Assert.Collection(persistedChild.Tags, persistedTag => Assert.Equal("Entertainment", persistedTag.Name));
        Assert.Equal(1, await CountRowsAsync(connection, "PaymentProfiles"));
        Assert.Equal(2, await CountRowsAsync(connection, "SubscriptionTags"));
    }

    [Fact]
    public async Task QueryFilter_HidesSoftDeletedSubscriptionButNotArchivedSubscription()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var deletedSubscription = CreateOrdinarySubscription("deleted@example.com");
        var archivedSubscription = CreateOrdinarySubscription("archived@example.com");
        var changedAtUtc = DateTimeOffset.UtcNow;
        deletedSubscription.SoftDelete(changedAtUtc);
        archivedSubscription.Archive(changedAtUtc);

        context.Subscriptions.AddRange(deletedSubscription, archivedSubscription);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var visibleSubscriptions = await context.Subscriptions.ToListAsync();
        var allSubscriptions = await context.Subscriptions
            .IgnoreQueryFilters()
            .ToListAsync();

        var visibleSubscription = Assert.Single(visibleSubscriptions);
        Assert.Equal(archivedSubscription.Id, visibleSubscription.Id);
        Assert.True(visibleSubscription.IsArchived);
        Assert.Equal(2, allSubscriptions.Count);
    }

    [Fact]
    public async Task SaveChangesAsync_RoundTripsOwnedBudgetValues()
    {
        await using var connection = await OpenConnectionAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var subscription = new OrdinarySubscription(
            "Crypto provider",
            "Storage plan",
            "Annual",
            "crypto@example.com",
            new Money(0.125m, "USDT", CurrencyKind.Custom),
            CreateSchedule());
        subscription.SetPaymentDeferralPolicy(
            new PaymentDeferralPolicy(7, 14),
            DateTimeOffset.UtcNow);

        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistedSubscription = await context.OrdinarySubscriptions
            .SingleAsync(candidate => candidate.Id == subscription.Id);

        Assert.Equal(0.125m, persistedSubscription.BillingAmount.Amount);
        Assert.Equal("USDT", persistedSubscription.BillingAmount.CurrencyCode);
        Assert.Equal(CurrencyKind.Custom, persistedSubscription.BillingAmount.CurrencyKind);
        Assert.Equal(7, persistedSubscription.PaymentDeferralPolicy?.ProviderGracePeriodDays);
        Assert.Equal(14, persistedSubscription.PaymentDeferralPolicy?.BudgetToleranceDays);
    }

    private static SubscriptionDbContext CreateContext(DbConnection connection)
    {
        var options = new DbContextOptionsBuilder<SubscriptionDbContext>()
            .UseSqlite(connection)
            .Options;

        return new SubscriptionDbContext(options);
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        return connection;
    }

    private static async Task<long> CountRowsAsync(SqliteConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";
        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt64(result);
    }

    private static OrdinarySubscription CreateOrdinarySubscription(string accountName)
    {
        return new OrdinarySubscription(
            "Google",
            "YouTube Premium",
            "Family",
            accountName,
            CreateMoney(),
            CreateSchedule());
    }

    private static Money CreateMoney() => new(19.99m, "USD", CurrencyKind.Iso4217);

    private static BillingSchedule CreateSchedule()
    {
        return new BillingSchedule(
            BillingCadence.Recurring,
            BillingIntervalUnit.Month,
            1,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 8, 1),
            null,
            true);
    }
}
