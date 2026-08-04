using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.ViewModels;

public sealed record PaymentProfileOption(string DisplayName, PaymentProfile? Value)
{
    public override string ToString() => DisplayName;
}
