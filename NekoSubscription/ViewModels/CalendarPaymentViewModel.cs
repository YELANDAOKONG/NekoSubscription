using System;

using CommunityToolkit.Mvvm.Input;

namespace NekoSubscription.ViewModels;

public sealed class CalendarPaymentViewModel
{
    public CalendarPaymentViewModel(CashFlowItemViewModel item, Action<Guid> openSubscription)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(openSubscription);

        Item = item;
        OpenSubscriptionCommand = new RelayCommand(() => openSubscription(item.SubscriptionId));
    }

    public CashFlowItemViewModel Item { get; }

    public IRelayCommand OpenSubscriptionCommand { get; }
}
