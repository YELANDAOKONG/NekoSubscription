using System;
using System.Globalization;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.ViewModels;

public sealed record SubscriptionListItemViewModel(
    Guid Id,
    string ServiceLabel,
    string AccountLabel,
    string CategoryLabel,
    string StatusLabel,
    string AmountLabel,
    string NextBillingLabel,
    string ImportanceLabel,
    bool IsArchived)
{
    public static SubscriptionListItemViewModel FromSubscription(Subscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var planSuffix = subscription.PlanName is null ? string.Empty : $" - {subscription.PlanName}";
        var accountLabel = subscription.AccountName ?? "No account specified";
        var statusLabel = subscription.IsArchived
            ? "Archived"
            : subscription.ConfirmationStatus switch
            {
                SubscriptionConfirmationStatus.ConfirmedActive => "Confirmed active",
                SubscriptionConfirmationStatus.ConfirmedInactive => "Confirmed inactive",
                SubscriptionConfirmationStatus.Unknown => "Status unknown",
                _ => "Invalid status"
            };
        var amount = subscription.BillingAmount.Amount.ToString(
            "0.##################",
            CultureInfo.CurrentCulture);
        var nextBillingOn = subscription.BillingSchedule.NextBillingOn ??
            subscription.BillingSchedule.StartsOn;

        return new SubscriptionListItemViewModel(
            subscription.Id,
            $"{subscription.ProviderName} / {subscription.ServiceName}{planSuffix}",
            accountLabel,
            FormatCategory(subscription.Category),
            statusLabel,
            $"{amount} {subscription.BillingAmount.CurrencyCode}",
            nextBillingOn?.ToString("d", CultureInfo.CurrentCulture) ?? "Not scheduled",
            subscription.Importance.ToString(),
            subscription.IsArchived);
    }

    private static string FormatCategory(SubscriptionCategory category)
    {
        return category switch
        {
            SubscriptionCategory.Ordinary => "Subscription",
            SubscriptionCategory.PhoneNumber => "Phone number",
            SubscriptionCategory.Domain => "Domain",
            SubscriptionCategory.CloudService => "Cloud service",
            SubscriptionCategory.Custom => "Custom",
            _ => "Unknown"
        };
    }
}
