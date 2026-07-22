using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.ViewModels;

public sealed record SubscriptionCategoryFilter(string DisplayName, SubscriptionCategory? Category)
{
    public override string ToString() => DisplayName;
}
