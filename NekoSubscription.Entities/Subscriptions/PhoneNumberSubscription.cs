using System;

namespace NekoSubscription.Entities.Subscriptions;

public sealed class PhoneNumberSubscription : Subscription
{
    public const int MaximumCarrierNameLength = 200;
    public const int MaximumPhoneNumberLength = 64;
    public const int MaximumRegionNameLength = 100;

    private PhoneNumberSubscription()
    {
    }

    public PhoneNumberSubscription(
        string providerName,
        string serviceName,
        string? planName,
        string? accountName,
        Money billingAmount,
        BillingSchedule billingSchedule,
        string phoneNumber,
        PhoneNumberType phoneNumberType,
        string carrierName,
        string? regionName,
        bool isPrepaid)
        : base(
            SubscriptionCategory.PhoneNumber,
            providerName,
            serviceName,
            planName,
            accountName,
            billingAmount,
            billingSchedule)
    {
        SetPhoneNumberDetails(phoneNumber, phoneNumberType, carrierName, regionName, isPrepaid, CreatedAtUtc);
    }

    public string PhoneNumber { get; private set; } = string.Empty;

    public PhoneNumberType PhoneNumberType { get; private set; }

    public string CarrierName { get; private set; } = string.Empty;

    public string? RegionName { get; private set; }

    public bool IsPrepaid { get; private set; }

    public void SetPhoneNumberDetails(
        string phoneNumber,
        PhoneNumberType phoneNumberType,
        string carrierName,
        string? regionName,
        bool isPrepaid,
        DateTimeOffset updatedAtUtc)
    {
        if (!Enum.IsDefined(phoneNumberType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(phoneNumberType),
                phoneNumberType,
                "The phone number type is invalid.");
        }

        PhoneNumber = NormalizeRequired(phoneNumber, MaximumPhoneNumberLength, nameof(phoneNumber));
        PhoneNumberType = phoneNumberType;
        CarrierName = NormalizeRequired(carrierName, MaximumCarrierNameLength, nameof(carrierName));
        RegionName = NormalizeOptional(regionName, MaximumRegionNameLength, nameof(regionName));
        IsPrepaid = isPrepaid;
        MarkUpdated(updatedAtUtc);
    }
}
