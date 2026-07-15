using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Entities.Tests.Subscriptions;

public sealed class BillingScheduleTests
{
    [Fact]
    public void Constructor_CreatesRecurringSchedule()
    {
        var startsOn = new DateOnly(2026, 1, 1);
        var nextBillingOn = new DateOnly(2026, 2, 1);
        var schedule = new BillingSchedule(
            BillingCadence.Recurring,
            BillingIntervalUnit.Month,
            1,
            startsOn,
            nextBillingOn,
            null,
            true);

        Assert.Equal(BillingCadence.Recurring, schedule.Cadence);
        Assert.Equal(BillingIntervalUnit.Month, schedule.IntervalUnit);
        Assert.Equal(1, schedule.IntervalCount);
        Assert.Equal(startsOn, schedule.StartsOn);
        Assert.Equal(nextBillingOn, schedule.NextBillingOn);
        Assert.True(schedule.AutomaticallyRenews);
    }

    [Fact]
    public void Constructor_RejectsRecurringScheduleWithoutInterval()
    {
        Assert.Throws<ArgumentException>(() => new BillingSchedule(
            BillingCadence.Recurring,
            null,
            null,
            null,
            null,
            null,
            false));
    }

    [Fact]
    public void Constructor_RejectsIntervalForManualSchedule()
    {
        Assert.Throws<ArgumentException>(() => new BillingSchedule(
            BillingCadence.Manual,
            BillingIntervalUnit.Month,
            1,
            null,
            null,
            null,
            false));
    }

    [Fact]
    public void Constructor_RejectsAutomaticRenewalForOneTimeSchedule()
    {
        Assert.Throws<ArgumentException>(() => new BillingSchedule(
            BillingCadence.OneTime,
            null,
            null,
            null,
            new DateOnly(2026, 1, 1),
            null,
            true));
    }

    [Fact]
    public void Constructor_RejectsNextBillingDateBeforeStartDate()
    {
        Assert.Throws<ArgumentException>(() => new BillingSchedule(
            BillingCadence.Recurring,
            BillingIntervalUnit.Year,
            1,
            new DateOnly(2026, 1, 1),
            new DateOnly(2025, 1, 1),
            null,
            true));
    }
}
