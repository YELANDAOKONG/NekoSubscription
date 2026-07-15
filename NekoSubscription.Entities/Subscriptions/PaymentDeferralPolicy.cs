using System;

namespace NekoSubscription.Entities.Subscriptions;

public sealed record PaymentDeferralPolicy
{
    private PaymentDeferralPolicy()
    {
    }

    public PaymentDeferralPolicy(int? providerGracePeriodDays, int? budgetToleranceDays)
    {
        if (providerGracePeriodDays is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(providerGracePeriodDays),
                providerGracePeriodDays,
                "The provider grace period cannot be negative.");
        }

        if (budgetToleranceDays is < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(budgetToleranceDays),
                budgetToleranceDays,
                "The budget tolerance cannot be negative.");
        }

        ProviderGracePeriodDays = providerGracePeriodDays;
        BudgetToleranceDays = budgetToleranceDays;
    }

    public int? ProviderGracePeriodDays { get; private set; }

    public int? BudgetToleranceDays { get; private set; }
}
