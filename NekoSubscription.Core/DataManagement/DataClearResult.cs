namespace NekoSubscription.Core.DataManagement;

public sealed record DataClearResult(
    int DeletedSubscriptionCount,
    int DeletedPaymentProfileCount,
    int DeletedTagCount);
