using System;
using System.Collections.Generic;

namespace NekoSubscription.Core.CashFlow;

public sealed record CashFlowProjection(
    DateOnly StartsOn,
    DateOnly EndsOn,
    IReadOnlyList<CashFlowItem> Items,
    IReadOnlyList<CashFlowCurrencyTotal> CurrencyTotals);
