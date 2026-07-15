using System;
using System.Collections.Generic;

namespace NekoSubscription.Entities.Subscriptions;

public abstract class Subscription
{
    public const int MaximumAccountNameLength = 320;
    public const int MaximumManagementUrlLength = 2048;
    public const int MaximumNotesLength = 4000;
    public const int MaximumPlanNameLength = 200;
    public const int MaximumProviderNameLength = 200;
    public const int MaximumServiceNameLength = 200;

    protected Subscription()
    {
        ProviderName = string.Empty;
        ServiceName = string.Empty;
        BillingAmount = null!;
        BillingSchedule = null!;
    }

    protected Subscription(
        SubscriptionCategory category,
        string providerName,
        string serviceName,
        string? planName,
        string? accountName,
        Money billingAmount,
        BillingSchedule billingSchedule)
    {
        ArgumentNullException.ThrowIfNull(billingAmount);
        ArgumentNullException.ThrowIfNull(billingSchedule);

        Id = Guid.NewGuid();
        Category = category;
        ProviderName = NormalizeRequired(providerName, MaximumProviderNameLength, nameof(providerName));
        ServiceName = NormalizeRequired(serviceName, MaximumServiceNameLength, nameof(serviceName));
        PlanName = NormalizeOptional(planName, MaximumPlanNameLength, nameof(planName));
        AccountName = NormalizeOptional(accountName, MaximumAccountNameLength, nameof(accountName));
        BillingAmount = billingAmount;
        BillingSchedule = billingSchedule;
        ConfirmationStatus = SubscriptionConfirmationStatus.Unknown;
        LifecycleStatus = SubscriptionLifecycleStatus.Unknown;
        Importance = SubscriptionImportance.Normal;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public SubscriptionCategory Category { get; private set; }

    public string ProviderName { get; private set; }

    public string ServiceName { get; private set; }

    public string? PlanName { get; private set; }

    public string? AccountName { get; private set; }

    public Money BillingAmount { get; private set; }

    public BillingSchedule BillingSchedule { get; private set; }

    public SubscriptionConfirmationStatus ConfirmationStatus { get; private set; }

    public SubscriptionLifecycleStatus LifecycleStatus { get; private set; }

    public SubscriptionImportance Importance { get; private set; }

    public PaymentDeferralPolicy? PaymentDeferralPolicy { get; private set; }

    public Guid? PaymentProfileId { get; private set; }

    public PaymentProfile? PaymentProfile { get; private set; }

    public Guid? IncludedWithSubscriptionId { get; private set; }

    public Subscription? IncludedWithSubscription { get; private set; }

    public ICollection<Subscription> IncludedSubscriptions { get; } = new List<Subscription>();

    public string? Notes { get; private set; }

    public string? ManagementUrl { get; private set; }

    public ICollection<Tag> Tags { get; } = new List<Tag>();

    public bool IsArchived { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool ParticipatesInBudget =>
        ConfirmationStatus == SubscriptionConfirmationStatus.ConfirmedActive && !IsDeleted;

    public void UpdateIdentity(
        string providerName,
        string serviceName,
        string? planName,
        string? accountName,
        DateTimeOffset updatedAtUtc)
    {
        ProviderName = NormalizeRequired(providerName, MaximumProviderNameLength, nameof(providerName));
        ServiceName = NormalizeRequired(serviceName, MaximumServiceNameLength, nameof(serviceName));
        PlanName = NormalizeOptional(planName, MaximumPlanNameLength, nameof(planName));
        AccountName = NormalizeOptional(accountName, MaximumAccountNameLength, nameof(accountName));
        MarkUpdated(updatedAtUtc);
    }

    public void UpdateBilling(Money billingAmount, BillingSchedule billingSchedule, DateTimeOffset updatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(billingAmount);
        ArgumentNullException.ThrowIfNull(billingSchedule);

        BillingAmount = billingAmount;
        BillingSchedule = billingSchedule;
        MarkUpdated(updatedAtUtc);
    }

    public void SetStatuses(
        SubscriptionConfirmationStatus confirmationStatus,
        SubscriptionLifecycleStatus lifecycleStatus,
        DateTimeOffset updatedAtUtc)
    {
        if (!Enum.IsDefined(confirmationStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(confirmationStatus),
                confirmationStatus,
                "The subscription confirmation status is invalid.");
        }

        if (!Enum.IsDefined(lifecycleStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifecycleStatus),
                lifecycleStatus,
                "The subscription lifecycle status is invalid.");
        }

        ConfirmationStatus = confirmationStatus;
        LifecycleStatus = lifecycleStatus;
        MarkUpdated(updatedAtUtc);
    }

    public void SetImportance(SubscriptionImportance importance, DateTimeOffset updatedAtUtc)
    {
        if (!Enum.IsDefined(importance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(importance),
                importance,
                "The subscription importance is invalid.");
        }

        Importance = importance;
        MarkUpdated(updatedAtUtc);
    }

    public void SetPaymentDeferralPolicy(
        PaymentDeferralPolicy? paymentDeferralPolicy,
        DateTimeOffset updatedAtUtc)
    {
        PaymentDeferralPolicy = paymentDeferralPolicy;
        MarkUpdated(updatedAtUtc);
    }

    public void SetPaymentProfile(PaymentProfile? paymentProfile, DateTimeOffset updatedAtUtc)
    {
        PaymentProfile = paymentProfile;
        PaymentProfileId = paymentProfile?.Id;
        MarkUpdated(updatedAtUtc);
    }

    public void SetIncludedWithSubscription(Subscription? sourceSubscription, DateTimeOffset updatedAtUtc)
    {
        if (sourceSubscription is not null)
        {
            EnsureBundleRelationshipHasNoCycle(sourceSubscription);
        }

        IncludedWithSubscription = sourceSubscription;
        IncludedWithSubscriptionId = sourceSubscription?.Id;
        MarkUpdated(updatedAtUtc);
    }

    public void UpdateNotesAndManagementUrl(
        string? notes,
        string? managementUrl,
        DateTimeOffset updatedAtUtc)
    {
        Notes = NormalizeOptional(notes, MaximumNotesLength, nameof(notes));
        ManagementUrl = NormalizeUrl(managementUrl);
        MarkUpdated(updatedAtUtc);
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        IsArchived = true;
        ArchivedAtUtc = archivedAtUtc;
        MarkUpdated(archivedAtUtc);
    }

    public void RestoreFromArchive(DateTimeOffset restoredAtUtc)
    {
        IsArchived = false;
        ArchivedAtUtc = null;
        MarkUpdated(restoredAtUtc);
    }

    public void SoftDelete(DateTimeOffset deletedAtUtc)
    {
        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        MarkUpdated(deletedAtUtc);
    }

    public void RestoreDeleted(DateTimeOffset restoredAtUtc)
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        MarkUpdated(restoredAtUtc);
    }

    protected void MarkUpdated(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
    }

    protected static string NormalizeRequired(string value, int maximumLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                normalizedValue.Length,
                $"The value cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    protected static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequired(value, maximumLength, parameterName);
    }

    private static string? NormalizeUrl(string? managementUrl)
    {
        var normalizedUrl = NormalizeOptional(
            managementUrl,
            MaximumManagementUrlLength,
            nameof(managementUrl));

        if (normalizedUrl is not null && !IsHttpUrl(normalizedUrl))
        {
            throw new ArgumentException("The management URL must use HTTP or HTTPS.", nameof(managementUrl));
        }

        return normalizedUrl;
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private void EnsureBundleRelationshipHasNoCycle(Subscription sourceSubscription)
    {
        var candidate = sourceSubscription;

        while (candidate is not null)
        {
            if (candidate.Id == Id)
            {
                throw new ArgumentException(
                    "A bundled subscription relationship cannot contain a cycle.",
                    nameof(sourceSubscription));
            }

            candidate = candidate.IncludedWithSubscription;
        }
    }
}
