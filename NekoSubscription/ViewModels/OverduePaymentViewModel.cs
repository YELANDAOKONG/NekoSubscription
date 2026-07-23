using System;
using System.Globalization;

using CommunityToolkit.Mvvm.Input;

using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public sealed class OverduePaymentViewModel
{
    public OverduePaymentViewModel(
        Subscription subscription,
        DateOnly dueOn,
        DateOnly today,
        Action<Guid> openSubscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(openSubscription);

        if (dueOn >= today)
        {
            throw new ArgumentException("An overdue payment must be earlier than today.", nameof(dueOn));
        }

        var listItem = SubscriptionListItemViewModel.FromSubscription(subscription);
        SubscriptionId = subscription.Id;
        ServiceLabel = listItem.ServiceLabel;
        ProviderLabel = listItem.ProviderLabel;
        AmountLabel = listItem.AmountLabel;
        DueOnLabel = dueOn.ToString("d", CultureInfo.CurrentCulture);
        DaysOverdueLabel = AppResources.Format(
            "Forecast_DaysOverdue",
            today.DayNumber - dueOn.DayNumber);
        OpenSubscriptionCommand = new RelayCommand(() => openSubscription(subscription.Id));
    }

    public string AmountLabel { get; }

    public string DaysOverdueLabel { get; }

    public string DueOnLabel { get; }

    public IRelayCommand OpenSubscriptionCommand { get; }

    public string ProviderLabel { get; }

    public string ServiceLabel { get; }

    public Guid SubscriptionId { get; }
}
