using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.DataManagement;

internal sealed record ImportedSubscriptionRow(
    string ProviderName,
    string ServiceName,
    string? AccountName,
    Money BillingAmount,
    BillingIntervalUnit IntervalUnit,
    int IntervalCount,
    DateOnly? StartsOn,
    DateOnly? NextBillingOn,
    bool IsActive,
    PaymentChannel PaymentChannel,
    string? PaymentAccount,
    string? Notes);
