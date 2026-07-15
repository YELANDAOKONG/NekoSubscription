using System.Collections.Generic;

namespace NekoSubscription.Entities.Subscriptions;

public sealed class CustomSubscription : Subscription
{
    private CustomSubscription()
    {
    }

    public CustomSubscription(
        string providerName,
        string serviceName,
        string? planName,
        string? accountName,
        Money billingAmount,
        BillingSchedule billingSchedule)
        : base(
            SubscriptionCategory.Custom,
            providerName,
            serviceName,
            planName,
            accountName,
            billingAmount,
            billingSchedule)
    {
    }

    public ICollection<CustomField> Fields { get; } = new List<CustomField>();
}
