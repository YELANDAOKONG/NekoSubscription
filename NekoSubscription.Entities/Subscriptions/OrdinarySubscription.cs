namespace NekoSubscription.Entities.Subscriptions;

public sealed class OrdinarySubscription : Subscription
{
    private OrdinarySubscription()
    {
    }

    public OrdinarySubscription(
        string providerName,
        string serviceName,
        string? planName,
        string? accountName,
        Money billingAmount,
        BillingSchedule billingSchedule)
        : base(
            SubscriptionCategory.Ordinary,
            providerName,
            serviceName,
            planName,
            accountName,
            billingAmount,
            billingSchedule)
    {
    }
}
