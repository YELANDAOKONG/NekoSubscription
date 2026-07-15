using System;

namespace NekoSubscription.Entities.Subscriptions;

public sealed class DomainSubscription : Subscription
{
    public const int MaximumDomainNameLength = 253;

    private DomainSubscription()
    {
    }

    public DomainSubscription(
        string registrarName,
        string serviceName,
        string? planName,
        string? registrarAccountName,
        Money billingAmount,
        BillingSchedule billingSchedule,
        string domainName,
        DateOnly? registeredOn,
        DateOnly? expiresOn)
        : base(
            SubscriptionCategory.Domain,
            registrarName,
            serviceName,
            planName,
            registrarAccountName,
            billingAmount,
            billingSchedule)
    {
        SetDomainDetails(domainName, registeredOn, expiresOn, CreatedAtUtc);
    }

    public string DomainName { get; private set; } = string.Empty;

    public DateOnly? RegisteredOn { get; private set; }

    public DateOnly? ExpiresOn { get; private set; }

    public void SetDomainDetails(
        string domainName,
        DateOnly? registeredOn,
        DateOnly? expiresOn,
        DateTimeOffset updatedAtUtc)
    {
        if (registeredOn is not null && expiresOn is not null && expiresOn < registeredOn)
        {
            throw new ArgumentException(
                "The domain expiration date cannot be earlier than the registration date.",
                nameof(expiresOn));
        }

        DomainName = NormalizeRequired(domainName, MaximumDomainNameLength, nameof(domainName))
            .ToLowerInvariant();
        RegisteredOn = registeredOn;
        ExpiresOn = expiresOn;
        MarkUpdated(updatedAtUtc);
    }
}
