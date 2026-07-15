using System;
using System.Linq;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Tests.CashFlow;

public sealed class CashFlowProjectorTests
{
    private readonly CashFlowProjector _projector = new();

    [Fact]
    public void Project_IncludesOnlySubscriptionsThatParticipateInBudget()
    {
        var included = CreateOrdinarySubscription(
            "included@example.com",
            CreateOneTimeSchedule(new DateOnly(2026, 8, 10)));
        var unknown = CreateOrdinarySubscription(
            "unknown@example.com",
            CreateOneTimeSchedule(new DateOnly(2026, 8, 10)),
            confirmActive: false);
        var deleted = CreateOrdinarySubscription(
            "deleted@example.com",
            CreateOneTimeSchedule(new DateOnly(2026, 8, 10)));
        deleted.SoftDelete(DateTimeOffset.UtcNow);

        var projection = _projector.Project(
            [included, unknown, deleted],
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31));

        var item = Assert.Single(projection.Items);
        Assert.Equal(included.Id, item.SubscriptionId);
        Assert.Single(projection.CurrencyTotals);
    }

    [Fact]
    public void Project_ExpandsDayAndWeekIntervalsFromAnchor()
    {
        var everyTwoDays = CreateOrdinarySubscription(
            "days@example.com",
            CreateRecurringSchedule(
                BillingIntervalUnit.Day,
                2,
                new DateOnly(2026, 1, 1)));
        var weekly = CreateOrdinarySubscription(
            "weeks@example.com",
            CreateRecurringSchedule(
                BillingIntervalUnit.Week,
                1,
                new DateOnly(2026, 1, 1)));

        var projection = _projector.Project(
            [everyTwoDays, weekly],
            new DateOnly(2026, 1, 5),
            new DateOnly(2026, 1, 15));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 5),
                new DateOnly(2026, 1, 7),
                new DateOnly(2026, 1, 9),
                new DateOnly(2026, 1, 11),
                new DateOnly(2026, 1, 13),
                new DateOnly(2026, 1, 15)
            ],
            projection.Items
                .Where(item => item.SubscriptionId == everyTwoDays.Id)
                .Select(item => item.ScheduledOn));
        Assert.Equal(
            [new DateOnly(2026, 1, 8), new DateOnly(2026, 1, 15)],
            projection.Items
                .Where(item => item.SubscriptionId == weekly.Id)
                .Select(item => item.ScheduledOn));
    }

    [Fact]
    public void Project_PreservesMonthEndAndLeapYearAnchor()
    {
        var monthly = CreateOrdinarySubscription(
            "monthly@example.com",
            CreateRecurringSchedule(
                BillingIntervalUnit.Month,
                1,
                new DateOnly(2026, 1, 31)));
        var yearly = CreateOrdinarySubscription(
            "yearly@example.com",
            CreateRecurringSchedule(
                BillingIntervalUnit.Year,
                1,
                new DateOnly(2024, 2, 29)));

        var monthlyProjection = _projector.Project(
            [monthly],
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 4, 30));
        var yearlyProjection = _projector.Project(
            [yearly],
            new DateOnly(2024, 1, 1),
            new DateOnly(2028, 3, 1));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31),
                new DateOnly(2026, 4, 30)
            ],
            monthlyProjection.Items.Select(item => item.ScheduledOn));
        Assert.Equal(
            [
                new DateOnly(2024, 2, 29),
                new DateOnly(2025, 2, 28),
                new DateOnly(2026, 2, 28),
                new DateOnly(2027, 2, 28),
                new DateOnly(2028, 2, 29)
            ],
            yearlyProjection.Items.Select(item => item.ScheduledOn));
    }

    [Fact]
    public void Project_HandlesKnownOneTimeAndManualDatesAndHonorsEndDate()
    {
        var oneTime = CreateOrdinarySubscription(
            "one-time@example.com",
            new BillingSchedule(
                BillingCadence.OneTime,
                null,
                null,
                new DateOnly(2026, 3, 15),
                null,
                null,
                false));
        var manual = CreateOrdinarySubscription(
            "manual@example.com",
            new BillingSchedule(
                BillingCadence.Manual,
                null,
                null,
                null,
                new DateOnly(2026, 3, 20),
                null,
                false));
        var undatedManual = CreateOrdinarySubscription(
            "undated@example.com",
            new BillingSchedule(BillingCadence.Manual, null, null, null, null, null, false));
        var recurring = CreateOrdinarySubscription(
            "ending@example.com",
            new BillingSchedule(
                BillingCadence.Recurring,
                BillingIntervalUnit.Month,
                1,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 3, 1),
                false));

        var projection = _projector.Project(
            [oneTime, manual, undatedManual, recurring],
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 4, 30));

        Assert.Equal(1, projection.Items.Count(item => item.SubscriptionId == oneTime.Id));
        Assert.Equal(1, projection.Items.Count(item => item.SubscriptionId == manual.Id));
        Assert.DoesNotContain(projection.Items, item => item.SubscriptionId == undatedManual.Id);
        Assert.Equal(
            [
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 2, 1),
                new DateOnly(2026, 3, 1)
            ],
            projection.Items
                .Where(item => item.SubscriptionId == recurring.Id)
                .Select(item => item.ScheduledOn));
    }

    [Fact]
    public void Project_AggregatesFixedAndEstimatedAmountsWithoutCurrencyConversion()
    {
        var scheduledOn = new DateOnly(2026, 9, 1);
        var fixedUsd = CreateOrdinarySubscription(
            "fixed@example.com",
            CreateOneTimeSchedule(scheduledOn),
            new Money(10m, "USD", CurrencyKind.Iso4217));
        var estimatedUsd = CreateCloudSubscription(
            "estimate@example.com",
            CreateOneTimeSchedule(scheduledOn),
            new Money(25m, "USD", CurrencyKind.Iso4217),
            CloudBillingMode.UsageBasedEstimate);
        var customUsd = CreateOrdinarySubscription(
            "custom-usd@example.com",
            CreateOneTimeSchedule(scheduledOn),
            new Money(1m, "USD", CurrencyKind.Custom));
        var bitcoin = CreateCloudSubscription(
            "bitcoin@example.com",
            CreateOneTimeSchedule(scheduledOn),
            new Money(0.5m, "BTC", CurrencyKind.Custom),
            CloudBillingMode.Fixed);

        var projection = _projector.Project(
            [fixedUsd, estimatedUsd, customUsd, bitcoin],
            scheduledOn,
            scheduledOn);

        Assert.Equal(3, projection.CurrencyTotals.Count);
        var standardUsd = Assert.Single(
            projection.CurrencyTotals,
            total => total.CurrencyCode == "USD" && total.CurrencyKind == CurrencyKind.Iso4217);
        Assert.Equal(10m, standardUsd.FixedAmount);
        Assert.Equal(25m, standardUsd.EstimatedAmount);
        Assert.Equal(35m, standardUsd.TotalAmount);

        var separateCustomUsd = Assert.Single(
            projection.CurrencyTotals,
            total => total.CurrencyCode == "USD" && total.CurrencyKind == CurrencyKind.Custom);
        Assert.Equal(1m, separateCustomUsd.TotalAmount);

        var bitcoinTotal = Assert.Single(
            projection.CurrencyTotals,
            total => total.CurrencyCode == "BTC");
        Assert.Equal(0.5m, bitcoinTotal.FixedAmount);
        Assert.Equal(0m, bitcoinTotal.EstimatedAmount);
    }

    [Fact]
    public void Project_PreservesMetadataAndCalculatesOptionalDeferralDates()
    {
        var scheduledOn = new DateOnly(2026, 10, 10);
        var parent = CreateOrdinarySubscription(
            "parent@example.com",
            CreateOneTimeSchedule(scheduledOn));
        var child = CreateOrdinarySubscription(
            "child@example.com",
            CreateOneTimeSchedule(scheduledOn));
        var changedAtUtc = DateTimeOffset.UtcNow;
        child.SetIncludedWithSubscription(parent, changedAtUtc);
        child.SetImportance(SubscriptionImportance.Essential, changedAtUtc);
        child.SetPaymentDeferralPolicy(new PaymentDeferralPolicy(5, 10), changedAtUtc);

        var projection = _projector.Project([child], scheduledOn, scheduledOn);

        var item = Assert.Single(projection.Items);
        Assert.Equal(new DateOnly(2026, 10, 15), item.ProviderGraceEndsOn);
        Assert.Equal(new DateOnly(2026, 10, 20), item.BudgetToleranceEndsOn);
        Assert.Equal(SubscriptionImportance.Essential, item.Importance);
        Assert.Equal(parent.Id, item.IncludedWithSubscriptionId);
        Assert.False(item.IsEstimate);
    }

    [Fact]
    public void Project_RejectsInvalidRange()
    {
        var exception = Assert.Throws<ArgumentException>(() => _projector.Project(
            [],
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 31)));

        Assert.Equal("endsOn", exception.ParamName);
    }

    private static OrdinarySubscription CreateOrdinarySubscription(
        string accountName,
        BillingSchedule schedule,
        Money? amount = null,
        bool confirmActive = true)
    {
        var subscription = new OrdinarySubscription(
            "Provider",
            "Service",
            "Plan",
            accountName,
            amount ?? new Money(10m, "USD", CurrencyKind.Iso4217),
            schedule);
        ConfirmIfRequested(subscription, confirmActive);
        return subscription;
    }

    private static CloudServiceSubscription CreateCloudSubscription(
        string accountName,
        BillingSchedule schedule,
        Money amount,
        CloudBillingMode billingMode)
    {
        var subscription = new CloudServiceSubscription(
            "Cloud provider",
            "Cloud service",
            null,
            accountName,
            amount,
            schedule,
            billingMode,
            null,
            null);
        ConfirmIfRequested(subscription, true);
        return subscription;
    }

    private static BillingSchedule CreateOneTimeSchedule(DateOnly scheduledOn)
    {
        return new BillingSchedule(
            BillingCadence.OneTime,
            null,
            null,
            null,
            scheduledOn,
            null,
            false);
    }

    private static BillingSchedule CreateRecurringSchedule(
        BillingIntervalUnit intervalUnit,
        int intervalCount,
        DateOnly nextBillingOn)
    {
        return new BillingSchedule(
            BillingCadence.Recurring,
            intervalUnit,
            intervalCount,
            null,
            nextBillingOn,
            null,
            true);
    }

    private static void ConfirmIfRequested(Subscription subscription, bool confirmActive)
    {
        if (!confirmActive)
        {
            return;
        }

        subscription.SetStatuses(
            SubscriptionConfirmationStatus.ConfirmedActive,
            SubscriptionLifecycleStatus.Active,
            DateTimeOffset.UtcNow);
    }
}
