using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.CashFlow;

public sealed record CashFlowCurrencyTotal(
    string CurrencyCode,
    CurrencyKind CurrencyKind,
    decimal FixedAmount,
    decimal EstimatedAmount)
{
    public decimal TotalAmount => FixedAmount + EstimatedAmount;
}
