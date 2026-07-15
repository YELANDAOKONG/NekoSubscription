using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.Subscriptions;

public sealed record SubscriptionQuery(
    SubscriptionCategory? Category = null,
    SubscriptionConfirmationStatus? ConfirmationStatus = null,
    SubscriptionLifecycleStatus? LifecycleStatus = null,
    string? CurrencyCode = null,
    string? AccountName = null,
    bool IncludeArchived = false,
    bool IncludeDeleted = false);
