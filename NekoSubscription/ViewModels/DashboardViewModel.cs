using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private const int ProjectionDayCount = 90;
    private const int UpcomingSubscriptionCount = 5;

    private readonly CashFlowProjector _cashFlowProjector;

    public DashboardViewModel(CashFlowProjector cashFlowProjector)
    {
        ArgumentNullException.ThrowIfNull(cashFlowProjector);
        _cashFlowProjector = cashFlowProjector;
    }

    public ObservableCollection<CurrencyTotalViewModel> CurrencyTotals { get; } = [];

    public ObservableCollection<SubscriptionListItemViewModel> UpcomingSubscriptions { get; } = [];

    public bool HasCurrencyTotals => CurrencyTotals.Count > 0;

    public bool HasNoCurrencyTotals => !HasCurrencyTotals;

    public bool HasUpcomingSubscriptions => UpcomingSubscriptions.Count > 0;

    public bool HasNoUpcomingSubscriptions => !HasUpcomingSubscriptions;

    [ObservableProperty]
    public partial int ActiveSubscriptionCount { get; private set; }

    [ObservableProperty]
    public partial int ArchivedSubscriptionCount { get; private set; }

    [ObservableProperty]
    public partial int TrialSubscriptionCount { get; private set; }

    [ObservableProperty]
    public partial int ProjectedPaymentCount { get; private set; }

    [ObservableProperty]
    public partial string NextPaymentLabel { get; private set; } =
        AppResources.Get("Common_NothingScheduled");

    public void Update(IReadOnlyList<Subscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        var visibleSubscriptions = subscriptions
            .Where(subscription => !subscription.IsArchived && !subscription.IsDeleted)
            .ToList();
        ActiveSubscriptionCount = visibleSubscriptions.Count(subscription =>
            subscription.LifecycleStatus is SubscriptionLifecycleStatus.Active or
                SubscriptionLifecycleStatus.Trial);
        ArchivedSubscriptionCount = subscriptions.Count(subscription => subscription.IsArchived);
        TrialSubscriptionCount = visibleSubscriptions.Count(subscription =>
            subscription.LifecycleStatus == SubscriptionLifecycleStatus.Trial);

        var projectionStartsOn = DateOnly.FromDateTime(DateTime.Today);
        var projectionEndsOn = projectionStartsOn.AddDays(ProjectionDayCount - 1);
        var projection = _cashFlowProjector.Project(
            visibleSubscriptions,
            projectionStartsOn,
            projectionEndsOn);
        ProjectedPaymentCount = projection.Items.Count;

        ReplaceItems(
            CurrencyTotals,
            projection.CurrencyTotals.Select(CurrencyTotalViewModel.FromTotal));

        var upcomingSubscriptions = visibleSubscriptions
            .Where(subscription => GetNextBillingOn(subscription) is not null)
            .OrderBy(subscription => GetNextBillingOn(subscription))
            .Take(UpcomingSubscriptionCount)
            .Select(SubscriptionListItemViewModel.FromSubscription);
        ReplaceItems(UpcomingSubscriptions, upcomingSubscriptions);

        NextPaymentLabel = UpcomingSubscriptions.FirstOrDefault()?.NextBillingLabel ??
            AppResources.Get("Common_NothingScheduled");
        OnPropertyChanged(nameof(HasCurrencyTotals));
        OnPropertyChanged(nameof(HasNoCurrencyTotals));
        OnPropertyChanged(nameof(HasUpcomingSubscriptions));
        OnPropertyChanged(nameof(HasNoUpcomingSubscriptions));
    }

    private static DateOnly? GetNextBillingOn(Subscription subscription) =>
        subscription.BillingSchedule.NextBillingOn ?? subscription.BillingSchedule.StartsOn;

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();

        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
