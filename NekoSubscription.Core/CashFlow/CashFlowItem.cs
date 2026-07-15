using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.CashFlow;

public sealed record CashFlowItem(
    Guid SubscriptionId,
    SubscriptionCategory Category,
    string ProviderName,
    string ServiceName,
    string? PlanName,
    string? AccountName,
    DateOnly ScheduledOn,
    DateOnly? ProviderGraceEndsOn,
    DateOnly? BudgetToleranceEndsOn,
    Money Amount,
    bool IsEstimate,
    SubscriptionImportance Importance,
    Guid? IncludedWithSubscriptionId);
