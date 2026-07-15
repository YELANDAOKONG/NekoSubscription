using System;

namespace NekoSubscription.Entities.Subscriptions;

public sealed record BillingSchedule
{
    private BillingSchedule()
    {
    }

    public BillingSchedule(
        BillingCadence cadence,
        BillingIntervalUnit? intervalUnit,
        int? intervalCount,
        DateOnly? startsOn,
        DateOnly? nextBillingOn,
        DateOnly? endsOn,
        bool automaticallyRenews)
    {
        if (!Enum.IsDefined(cadence))
        {
            throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "The billing cadence is invalid.");
        }

        ValidateInterval(cadence, intervalUnit, intervalCount);
        ValidateDates(startsOn, nextBillingOn, endsOn);

        if (automaticallyRenews && cadence != BillingCadence.Recurring)
        {
            throw new ArgumentException(
                "Only a recurring billing schedule can renew automatically.",
                nameof(automaticallyRenews));
        }

        Cadence = cadence;
        IntervalUnit = intervalUnit;
        IntervalCount = intervalCount;
        StartsOn = startsOn;
        NextBillingOn = nextBillingOn;
        EndsOn = endsOn;
        AutomaticallyRenews = automaticallyRenews;
    }

    public BillingCadence Cadence { get; private set; }

    public BillingIntervalUnit? IntervalUnit { get; private set; }

    public int? IntervalCount { get; private set; }

    public DateOnly? StartsOn { get; private set; }

    public DateOnly? NextBillingOn { get; private set; }

    public DateOnly? EndsOn { get; private set; }

    public bool AutomaticallyRenews { get; private set; }

    private static void ValidateInterval(
        BillingCadence cadence,
        BillingIntervalUnit? intervalUnit,
        int? intervalCount)
    {
        if (cadence == BillingCadence.Recurring)
        {
            if (intervalUnit is null || intervalCount is null or <= 0)
            {
                throw new ArgumentException(
                    "A recurring billing schedule requires a positive interval and an interval unit.",
                    nameof(intervalCount));
            }

            if (!Enum.IsDefined(intervalUnit.Value))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(intervalUnit),
                    intervalUnit,
                    "The billing interval unit is invalid.");
            }

            return;
        }

        if (intervalUnit is not null || intervalCount is not null)
        {
            throw new ArgumentException(
                "Only a recurring billing schedule can have an interval.",
                nameof(intervalCount));
        }
    }

    private static void ValidateDates(DateOnly? startsOn, DateOnly? nextBillingOn, DateOnly? endsOn)
    {
        if (startsOn is not null && endsOn is not null && endsOn < startsOn)
        {
            throw new ArgumentException("The end date cannot be earlier than the start date.", nameof(endsOn));
        }

        if (startsOn is not null && nextBillingOn is not null && nextBillingOn < startsOn)
        {
            throw new ArgumentException(
                "The next billing date cannot be earlier than the start date.",
                nameof(nextBillingOn));
        }
    }
}
