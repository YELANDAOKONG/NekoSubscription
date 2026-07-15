using System;
using System.Globalization;

using NekoSubscription.Core.CashFlow;

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
            $"{total.CurrencyCode} ({total.CurrencyKind})",
            FormatAmount("Fixed", total.FixedAmount),
            FormatAmount("Estimated", total.EstimatedAmount),
            FormatAmount("Total", total.TotalAmount));
    }

    private static string FormatAmount(string label, decimal amount)
    {
        var formattedAmount = amount.ToString("0.##################", CultureInfo.CurrentCulture);
        return $"{label}: {formattedAmount}";
    }
}
