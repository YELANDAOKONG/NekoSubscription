using System;
using System.Globalization;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public sealed record CurrencyTotalViewModel(
    string CurrencyLabel,
    string FixedAmountLabel,
    string EstimatedAmountLabel,
    string TotalAmountLabel)
{
    public static CurrencyTotalViewModel FromTotal(CashFlowCurrencyTotal total)
    {
        ArgumentNullException.ThrowIfNull(total);

        return new CurrencyTotalViewModel(
            $"{total.CurrencyCode} ({FormatCurrencyKind(total.CurrencyKind)})",
            FormatAmount(total.FixedAmount),
            FormatAmount(total.EstimatedAmount),
            FormatAmount(total.TotalAmount));
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static string FormatCurrencyKind(CurrencyKind currencyKind) => currencyKind switch
    {
        CurrencyKind.Iso4217 => AppResources.Get("CurrencyKind_Iso4217"),
        CurrencyKind.Custom => AppResources.Get("CurrencyKind_Custom"),
        _ => AppResources.Get("Common_Unknown")
    };
}
