using System;
using System.Globalization;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public sealed record CashFlowItemViewModel(
    Guid SubscriptionId,
    string ServiceLabel,
    string ProviderLabel,
    string AmountLabel,
    string ScheduledOnLabel,
    DateOnly ScheduledOn,
    string AmountKindLabel,
    bool IsEstimate)
{
    public static CashFlowItemViewModel FromItem(CashFlowItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var serviceLabel = item.PlanName is null
            ? item.ServiceName
            : $"{item.ServiceName} · {item.PlanName}";
        var amount = item.Amount.Amount.ToString("0.##", CultureInfo.CurrentCulture);

        return new CashFlowItemViewModel(
            item.SubscriptionId,
            serviceLabel,
            item.ProviderName,
            $"{amount} {item.Amount.CurrencyCode}",
            item.ScheduledOn.ToString("d", CultureInfo.CurrentCulture),
            item.ScheduledOn,
            AppResources.Get(item.IsEstimate ? "Forecast_Estimated" : "Forecast_Fixed"),
            item.IsEstimate);
    }
}
