using System;

namespace NekoSubscription.Entities.Subscriptions;

public sealed class CloudServiceSubscription : Subscription
{
    public const int MaximumProjectIdentifierLength = 256;
    public const int MaximumTenantIdentifierLength = 256;

    private CloudServiceSubscription()
    {
    }

    public CloudServiceSubscription(
        string providerName,
        string serviceName,
        string? planName,
        string? accountName,
        Money billingAmount,
        BillingSchedule billingSchedule,
        CloudBillingMode billingMode,
        string? tenantIdentifier,
        string? projectIdentifier)
        : base(
            SubscriptionCategory.CloudService,
            providerName,
            serviceName,
            planName,
            accountName,
            billingAmount,
            billingSchedule)
    {
        SetCloudDetails(billingMode, tenantIdentifier, projectIdentifier, CreatedAtUtc);
    }

    public CloudBillingMode BillingMode { get; private set; }

    public string? TenantIdentifier { get; private set; }

    public string? ProjectIdentifier { get; private set; }

    public void SetCloudDetails(
        CloudBillingMode billingMode,
        string? tenantIdentifier,
        string? projectIdentifier,
        DateTimeOffset updatedAtUtc)
    {
        if (!Enum.IsDefined(billingMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(billingMode),
                billingMode,
                "The cloud billing mode is invalid.");
        }

        BillingMode = billingMode;
        TenantIdentifier = NormalizeOptional(
            tenantIdentifier,
            MaximumTenantIdentifierLength,
            nameof(tenantIdentifier));
        ProjectIdentifier = NormalizeOptional(
            projectIdentifier,
            MaximumProjectIdentifierLength,
            nameof(projectIdentifier));
        MarkUpdated(updatedAtUtc);
    }
}
