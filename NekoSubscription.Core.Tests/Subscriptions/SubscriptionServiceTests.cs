using System;
using System.IO;
using System.Threading.Tasks;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.Subscriptions;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Tests.Subscriptions;

public sealed class SubscriptionServiceTests : IDisposable
{
    private const string ApplicationDirectoryName = "NekoSubscription";
    private const string ConfigurationDatabaseFileName = "configuration.db";
    private const string DataDatabaseFileName = "data.db";

    private readonly string _dataRootDirectory;
    private readonly SubscriptionService _service;

    public SubscriptionServiceTests()
    {
        _dataRootDirectory = Path.Combine(
            Path.GetTempPath(),
            "NekoSubscription.Core.Tests",
            Guid.NewGuid().ToString("N"));
        var applicationDataDirectory = Path.Combine(_dataRootDirectory, ApplicationDirectoryName);
        var paths = new ApplicationStoragePaths(
            _dataRootDirectory,
            applicationDataDirectory,
            Path.Combine(applicationDataDirectory, ConfigurationDatabaseFileName),
            Path.Combine(applicationDataDirectory, DataDatabaseFileName),
            Path.Combine(applicationDataDirectory, "logs"),
            Path.Combine(applicationDataDirectory, "crash-reports"));
        _service = new SubscriptionService(paths);
    }

    [Fact]
    public async Task GetSubscriptionsAsync_LoadsCompleteAggregateAndAppliesFilters()
    {
        var paymentProfile = new PaymentProfile(
            "Primary Google account",
            PaymentChannel.GooglePlay,
            "billing@example.com",
            "Google",
            null);
        var tag = new Tag("Entertainment");
        var parent = CreateOrdinarySubscription("parent@example.com");
        var child = new CustomSubscription(
            "Google",
            "YouTube Premium",
            "Family",
            "child@example.com",
            CreateMoney(),
            CreateSchedule());
        child.Fields.Add(CustomField.CreateText("Region", "Taiwan"));

        await _service.AddPaymentProfileAsync(paymentProfile);
        await _service.AddTagAsync(tag);
        await _service.AddSubscriptionAsync(parent);
        await _service.AddSubscriptionAsync(child);
        await _service.UpdateSubscriptionAsync(
            child.Id,
            (subscription, changedAtUtc) => subscription.SetStatuses(
                SubscriptionConfirmationStatus.ConfirmedActive,
                SubscriptionLifecycleStatus.Active,
                changedAtUtc));
        await _service.SetPaymentProfileAsync(child.Id, paymentProfile.Id);
        await _service.SetIncludedWithSubscriptionAsync(child.Id, parent.Id);
        await _service.SetTagsAsync(child.Id, [tag.Id]);

        var results = await _service.GetSubscriptionsAsync(
            new SubscriptionQuery(
                Category: SubscriptionCategory.Custom,
                ConfirmationStatus: SubscriptionConfirmationStatus.ConfirmedActive,
                LifecycleStatus: SubscriptionLifecycleStatus.Active,
                CurrencyCode: " usd ",
                AccountName: " child@example.com "));

        var persistedChild = Assert.IsType<CustomSubscription>(Assert.Single(results));
        Assert.Equal(paymentProfile.Id, persistedChild.PaymentProfile?.Id);
        Assert.Equal(parent.Id, persistedChild.IncludedWithSubscription?.Id);
        Assert.Collection(persistedChild.Tags, persistedTag => Assert.Equal(tag.Id, persistedTag.Id));
        Assert.Collection(
            persistedChild.Fields,
            field =>
            {
                Assert.Equal("Region", field.Name);
                Assert.Equal("Taiwan", field.TextValue);
            });
    }

    [Fact]
    public async Task ArchiveAndSoftDelete_AreFilteredAndRestoredIndependently()
    {
        var archivedSubscription = CreateOrdinarySubscription("archived@example.com");
        var deletedSubscription = CreateOrdinarySubscription("deleted@example.com");
        await _service.AddSubscriptionAsync(archivedSubscription);
        await _service.AddSubscriptionAsync(deletedSubscription);

        Assert.True(await _service.ArchiveSubscriptionAsync(archivedSubscription.Id));
        Assert.True(await _service.SoftDeleteSubscriptionAsync(deletedSubscription.Id));

        Assert.Empty(await _service.GetSubscriptionsAsync());

        var withArchived = await _service.GetSubscriptionsAsync(
            new SubscriptionQuery(IncludeArchived: true));
        Assert.Equal(archivedSubscription.Id, Assert.Single(withArchived).Id);

        var withDeleted = await _service.GetSubscriptionsAsync(
            new SubscriptionQuery(IncludeArchived: true, IncludeDeleted: true));
        Assert.Equal(2, withDeleted.Count);
        Assert.Contains(withDeleted, subscription => subscription.IsArchived && !subscription.IsDeleted);
        Assert.Contains(withDeleted, subscription => subscription.IsDeleted && !subscription.IsArchived);

        Assert.True(await _service.RestoreSubscriptionFromArchiveAsync(archivedSubscription.Id));
        Assert.True(await _service.RestoreDeletedSubscriptionAsync(deletedSubscription.Id));
        Assert.Equal(2, (await _service.GetSubscriptionsAsync()).Count);
    }

    [Fact]
    public async Task UpdateAndPaymentProfileLifecycle_PersistChanges()
    {
        var subscription = CreateOrdinarySubscription("before@example.com");
        var paymentProfile = new PaymentProfile(
            "PayPal account",
            PaymentChannel.PayPal,
            "payments@example.com",
            "PayPal",
            null);
        await _service.AddSubscriptionAsync(subscription);
        await _service.AddPaymentProfileAsync(paymentProfile);

        var updated = await _service.UpdateSubscriptionAsync(
            subscription.Id,
            (persistedSubscription, changedAtUtc) =>
            {
                persistedSubscription.UpdateIdentity(
                    "Google",
                    "YouTube Premium",
                    "Individual",
                    "after@example.com",
                    changedAtUtc);
                persistedSubscription.SetImportance(SubscriptionImportance.Essential, changedAtUtc);
            });
        var profileUpdated = await _service.UpdatePaymentProfileAsync(
            paymentProfile.Id,
            (profile, changedAtUtc) => profile.Update(
                "Household PayPal",
                PaymentChannel.PayPal,
                "payments@example.com",
                "PayPal",
                "Shared account",
                changedAtUtc));

        Assert.True(updated);
        Assert.True(profileUpdated);
        var persistedSubscription = Assert.IsType<OrdinarySubscription>(
            await _service.GetSubscriptionAsync(subscription.Id));
        Assert.Equal("Individual", persistedSubscription.PlanName);
        Assert.Equal("after@example.com", persistedSubscription.AccountName);
        Assert.Equal(SubscriptionImportance.Essential, persistedSubscription.Importance);

        Assert.True(await _service.ArchivePaymentProfileAsync(paymentProfile.Id));
        Assert.Empty(await _service.GetPaymentProfilesAsync());
        Assert.True(await _service.RestorePaymentProfileFromArchiveAsync(paymentProfile.Id));
        var persistedProfile = Assert.Single(await _service.GetPaymentProfilesAsync());
        Assert.Equal("Household PayPal", persistedProfile.DisplayName);
        Assert.Equal("Shared account", persistedProfile.Notes);
    }

    [Fact]
    public async Task SetIncludedWithSubscriptionAsync_RejectsIndirectCycle()
    {
        var first = CreateOrdinarySubscription("first@example.com");
        var second = CreateOrdinarySubscription("second@example.com");
        var third = CreateOrdinarySubscription("third@example.com");
        await _service.AddSubscriptionAsync(first);
        await _service.AddSubscriptionAsync(second);
        await _service.AddSubscriptionAsync(third);
        Assert.True(await _service.SetIncludedWithSubscriptionAsync(second.Id, first.Id));
        Assert.True(await _service.SetIncludedWithSubscriptionAsync(third.Id, second.Id));

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.SetIncludedWithSubscriptionAsync(first.Id, third.Id));

        Assert.Equal("sourceSubscriptionId", exception.ParamName);
        Assert.Null((await _service.GetSubscriptionAsync(first.Id))?.IncludedWithSubscriptionId);
    }

    public void Dispose()
    {
        _service.Dispose();

        if (Directory.Exists(_dataRootDirectory))
        {
            Directory.Delete(_dataRootDirectory, true);
        }
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
