using System;
using System.Collections.Generic;
using System.Linq;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.CashFlow;

public sealed class CashFlowProjector
{
    private const int MonthsPerYear = 12;
    private const int DaysPerWeek = 7;

    public CashFlowProjection Project(
        IEnumerable<Subscription> subscriptions,
        DateOnly startsOn,
        DateOnly endsOn)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        if (endsOn < startsOn)
        {
            throw new ArgumentException(
                "The projection end date cannot be earlier than the start date.",
                nameof(endsOn));
        }

        var subscriptionList = subscriptions.ToArray();
        if (subscriptionList.Any(subscription => subscription is null))
        {
            throw new ArgumentException("The subscription collection cannot contain null values.", nameof(subscriptions));
        }

        var items = subscriptionList
            .Where(subscription => subscription.ParticipatesInBudget)
            .SelectMany(subscription => CreateItems(subscription, startsOn, endsOn))
            .OrderBy(item => item.ScheduledOn)
            .ThenBy(item => item.Amount.CurrencyCode)
            .ThenBy(item => item.ProviderName)
            .ThenBy(item => item.ServiceName)
            .ThenBy(item => item.AccountName)
            .ThenBy(item => item.SubscriptionId)
            .ToArray();

        var currencyTotals = items
            .GroupBy(item => new { item.Amount.CurrencyCode, item.Amount.CurrencyKind })
            .Select(group => new CashFlowCurrencyTotal(
                group.Key.CurrencyCode,
                group.Key.CurrencyKind,
                group.Where(item => !item.IsEstimate).Sum(item => item.Amount.Amount),
                group.Where(item => item.IsEstimate).Sum(item => item.Amount.Amount)))
            .OrderBy(total => total.CurrencyCode)
            .ThenBy(total => total.CurrencyKind)
            .ToArray();

        return new CashFlowProjection(startsOn, endsOn, items, currencyTotals);
    }

    private static IEnumerable<CashFlowItem> CreateItems(
        Subscription subscription,
        DateOnly startsOn,
        DateOnly endsOn)
    {
        foreach (var scheduledOn in GetScheduledDates(subscription.BillingSchedule, startsOn, endsOn))
        {
            yield return new CashFlowItem(
                subscription.Id,
                subscription.Category,
                subscription.ProviderName,
                subscription.ServiceName,
                subscription.PlanName,
                subscription.AccountName,
                scheduledOn,
                AddOptionalDays(
                    scheduledOn,
                    subscription.PaymentDeferralPolicy?.ProviderGracePeriodDays),
                AddOptionalDays(
                    scheduledOn,
                    subscription.PaymentDeferralPolicy?.BudgetToleranceDays),
                subscription.BillingAmount,
                IsEstimatedAmount(subscription),
                subscription.Importance,
                subscription.IncludedWithSubscriptionId);
        }
    }

    private static IEnumerable<DateOnly> GetScheduledDates(
        BillingSchedule schedule,
        DateOnly startsOn,
        DateOnly endsOn)
    {
        var effectiveEndsOn = schedule.EndsOn is { } scheduleEndsOn && scheduleEndsOn < endsOn
            ? scheduleEndsOn
            : endsOn;
        if (effectiveEndsOn < startsOn)
        {
            yield break;
        }

        var anchorDate = schedule.NextBillingOn ?? schedule.StartsOn;
        if (anchorDate is null)
        {
            yield break;
        }

        switch (schedule.Cadence)
        {
            case BillingCadence.OneTime:
            case BillingCadence.Manual:
                if (anchorDate >= startsOn && anchorDate <= effectiveEndsOn)
                {
                    yield return anchorDate.Value;
                }

                yield break;
            case BillingCadence.Recurring:
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(schedule),
                    schedule.Cadence,
                    "The billing cadence is invalid.");
        }

        if (schedule.IntervalUnit is null || schedule.IntervalCount is null or <= 0)
        {
            throw new ArgumentException(
                "A recurring billing schedule requires a positive interval and an interval unit.",
                nameof(schedule));
        }

        var occurrenceIndex = GetInitialOccurrenceIndex(
            anchorDate.Value,
            startsOn,
            schedule.IntervalUnit.Value,
            schedule.IntervalCount.Value);

        while (GetOccurrenceDate(
                   anchorDate.Value,
                   schedule.IntervalUnit.Value,
                   schedule.IntervalCount.Value,
                   occurrenceIndex) is { } occurrenceDate &&
               occurrenceDate <= effectiveEndsOn)
        {
            if (occurrenceDate >= startsOn)
            {
                yield return occurrenceDate;
            }

            occurrenceIndex++;
        }
    }

    private static long GetInitialOccurrenceIndex(
        DateOnly anchorDate,
        DateOnly startsOn,
        BillingIntervalUnit intervalUnit,
        int intervalCount)
    {
        if (startsOn <= anchorDate)
        {
            return 0;
        }

        return intervalUnit switch
        {
            BillingIntervalUnit.Day => DivideRoundingUp(
                startsOn.DayNumber - anchorDate.DayNumber,
                intervalCount),
            BillingIntervalUnit.Week => DivideRoundingUp(
                startsOn.DayNumber - anchorDate.DayNumber,
                (long)intervalCount * DaysPerWeek),
            BillingIntervalUnit.Month => GetMonthDifference(anchorDate, startsOn) / intervalCount,
            BillingIntervalUnit.Year => (startsOn.Year - anchorDate.Year) / intervalCount,
            _ => throw new ArgumentOutOfRangeException(
                nameof(intervalUnit),
                intervalUnit,
                "The billing interval unit is invalid.")
        };
    }

    private static DateOnly? GetOccurrenceDate(
        DateOnly anchorDate,
        BillingIntervalUnit intervalUnit,
        int intervalCount,
        long occurrenceIndex)
    {
        if (!Enum.IsDefined(intervalUnit))
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalUnit),
                intervalUnit,
                "The billing interval unit is invalid.");
        }

        var intervalMultiplier = (long)intervalCount * occurrenceIndex;
        var unitsToAdd = intervalUnit == BillingIntervalUnit.Week
            ? intervalMultiplier * DaysPerWeek
            : intervalMultiplier;
        if (unitsToAdd > int.MaxValue)
        {
            return null;
        }

        try
        {
            return intervalUnit switch
            {
                BillingIntervalUnit.Day => anchorDate.AddDays((int)unitsToAdd),
                BillingIntervalUnit.Week => anchorDate.AddDays((int)unitsToAdd),
                BillingIntervalUnit.Month => anchorDate.AddMonths((int)unitsToAdd),
                BillingIntervalUnit.Year => anchorDate.AddYears((int)unitsToAdd),
                _ => throw new InvalidOperationException("The billing interval unit was not handled.")
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static int GetMonthDifference(DateOnly earlierDate, DateOnly laterDate) =>
        ((laterDate.Year - earlierDate.Year) * MonthsPerYear) + laterDate.Month - earlierDate.Month;

    private static long DivideRoundingUp(long dividend, long divisor) =>
        (dividend + divisor - 1) / divisor;

    private static DateOnly? AddOptionalDays(DateOnly scheduledOn, int? days)
    {
        if (days is null)
        {
            return null;
        }

        try
        {
            return scheduledOn.AddDays(days.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return DateOnly.MaxValue;
        }
    }

    private static bool IsEstimatedAmount(Subscription subscription) =>
        subscription is CloudServiceSubscription { BillingMode: CloudBillingMode.UsageBasedEstimate };
}
