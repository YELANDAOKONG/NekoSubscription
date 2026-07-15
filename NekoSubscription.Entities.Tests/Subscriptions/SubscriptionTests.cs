using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Entities.Tests.Subscriptions;

public sealed class SubscriptionTests
{
    [Fact]
    public void Constructor_UsesExpectedDefaults()
    {
        var subscription = CreateSubscription("account@example.com");

        Assert.Equal(SubscriptionCategory.Ordinary, subscription.Category);
        Assert.Equal(SubscriptionConfirmationStatus.Unknown, subscription.ConfirmationStatus);
        Assert.Equal(SubscriptionLifecycleStatus.Unknown, subscription.LifecycleStatus);
        Assert.Equal(SubscriptionImportance.Normal, subscription.Importance);
        Assert.False(subscription.ParticipatesInBudget);
        Assert.False(subscription.IsArchived);
        Assert.False(subscription.IsDeleted);
    }

    [Fact]
    public void ConfirmedActiveSubscription_ParticipatesInBudgetUntilSoftDeleted()
    {
        var subscription = CreateSubscription("primary@example.com");
        var changedAtUtc = DateTimeOffset.UtcNow;

        subscription.SetStatuses(
            SubscriptionConfirmationStatus.ConfirmedActive,
            SubscriptionLifecycleStatus.Active,
            changedAtUtc);

        Assert.True(subscription.ParticipatesInBudget);

        subscription.SoftDelete(changedAtUtc.AddMinutes(1));

        Assert.False(subscription.ParticipatesInBudget);

        subscription.RestoreDeleted(changedAtUtc.AddMinutes(2));

        Assert.True(subscription.ParticipatesInBudget);
    }

    [Fact]
    public void Archive_DoesNotSoftDeleteSubscription()
    {
        var subscription = CreateSubscription("archive@example.com");
        var archivedAtUtc = DateTimeOffset.UtcNow;

        subscription.Archive(archivedAtUtc);

        Assert.True(subscription.IsArchived);
        Assert.Equal(archivedAtUtc, subscription.ArchivedAtUtc);
        Assert.False(subscription.IsDeleted);
        Assert.Null(subscription.DeletedAtUtc);

        subscription.RestoreFromArchive(archivedAtUtc.AddMinutes(1));

        Assert.False(subscription.IsArchived);
        Assert.Null(subscription.ArchivedAtUtc);
    }

    [Fact]
    public void SetIncludedWithSubscription_RejectsCycle()
    {
        var parent = CreateSubscription("parent@example.com");
        var child = CreateSubscription("child@example.com");
        var changedAtUtc = DateTimeOffset.UtcNow;

        child.SetIncludedWithSubscription(parent, changedAtUtc);

        var exception = Assert.Throws<ArgumentException>(
            () => parent.SetIncludedWithSubscription(child, changedAtUtc));

        Assert.Equal("sourceSubscription", exception.ParamName);
    }

    [Fact]
    public void SeparateServiceAccounts_ArePreserved()
    {
        var first = CreateSubscription("first@example.com");
        var second = CreateSubscription("second@example.com");

        Assert.Equal(first.ProviderName, second.ProviderName);
        Assert.Equal(first.ServiceName, second.ServiceName);
        Assert.NotEqual(first.AccountName, second.AccountName);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void UpdateNotesAndManagementUrl_RejectsNonWebUrl()
    {
        var subscription = CreateSubscription("account@example.com");

        var exception = Assert.Throws<ArgumentException>(
            () => subscription.UpdateNotesAndManagementUrl(null, "/relative/path", DateTimeOffset.UtcNow));

        Assert.Equal("managementUrl", exception.ParamName);
    }

    private static OrdinarySubscription CreateSubscription(string accountName)
    {
        return new OrdinarySubscription(
            "Google",
            "YouTube Premium",
            "Family",
            accountName,
            new Money(22.99m, "USD", CurrencyKind.Iso4217),
            new BillingSchedule(
                BillingCadence.Recurring,
                BillingIntervalUnit.Month,
                1,
                null,
                new DateOnly(2026, 8, 1),
                null,
                true));
    }
}
