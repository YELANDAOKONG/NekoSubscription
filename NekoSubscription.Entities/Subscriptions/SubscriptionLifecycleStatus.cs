namespace NekoSubscription.Entities.Subscriptions;

public enum SubscriptionLifecycleStatus
{
    Unknown,
    Trial,
    Active,
    Paused,
    CancellationScheduled,
    Cancelled,
    Expired
}
