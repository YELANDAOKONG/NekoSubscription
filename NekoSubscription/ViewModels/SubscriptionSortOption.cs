namespace NekoSubscription.ViewModels;

public enum SubscriptionSortType
{
    NextBillingAscending,
    NextBillingDescending,
    AmountDescending,
    AmountAscending,
    NameAscending,
    NameDescending
}

public sealed record SubscriptionSortOption(string DisplayName, SubscriptionSortType SortType)
{
    public override string ToString() => DisplayName;
}
