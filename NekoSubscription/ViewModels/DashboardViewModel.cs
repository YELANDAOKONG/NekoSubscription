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
    private const int DefaultProjectionDayCount = 7;
    private const int MaximumUpcomingPaymentCount = 6;
    private const int ThreeDayProjection = 3;
    private const int SevenDayProjection = 7;
    private const int FourteenDayProjection = 14;
    private const int ThirtyDayProjection = 30;

    private readonly CashFlowProjector _cashFlowProjector;
    private IReadOnlyList<Subscription> _subscriptions = [];
    private int _projectionDayCount = DefaultProjectionDayCount;

    public DashboardViewModel(CashFlowProjector cashFlowProjector)
    {
        ArgumentNullException.ThrowIfNull(cashFlowProjector);
        _cashFlowProjector = cashFlowProjector;
        BuildForecastPeriods();
    }

    public ObservableCollection<CurrencyTotalViewModel> CurrencyTotals { get; } = [];

    public ObservableCollection<ForecastPeriodOptionViewModel> ForecastPeriods { get; } = [];

    public ObservableCollection<CashFlowItemViewModel> UpcomingPayments { get; } = [];

    public bool HasCurrencyTotals => CurrencyTotals.Count > 0;

    public bool HasNoCurrencyTotals => !HasCurrencyTotals;

    public bool HasUpcomingPayments => UpcomingPayments.Count > 0;

    public bool HasNoUpcomingPayments => !HasUpcomingPayments;

    public bool HasExcludedSubscriptions => ExcludedSubscriptionCount > 0;

    public string ExcludedSubscriptionLabel => AppResources.Format(
        "Forecast_ExcludedSubscriptions",
        ExcludedSubscriptionCount);

    public string ProjectionPeriodLabel => AppResources.Format(
        "Forecast_PeriodLabel",
        _projectionDayCount);

    [ObservableProperty]
    public partial int ActiveSubscriptionCount { get; private set; }

    [ObservableProperty]
    public partial int ArchivedSubscriptionCount { get; private set; }

    [ObservableProperty]
    public partial int TrialSubscriptionCount { get; private set; }

    [ObservableProperty]
    public partial int ProjectedPaymentCount { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasExcludedSubscriptions))]
    public partial int ExcludedSubscriptionCount { get; private set; }

    [ObservableProperty]
    public partial string NextPaymentLabel { get; private set; } =
        AppResources.Get("Common_NothingScheduled");

    public void RefreshLocalization()
    {
        BuildForecastPeriods();
        Recalculate();
    }

    public void Update(IReadOnlyList<Subscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        _subscriptions = subscriptions;
        if (ForecastPeriods.Count == 0)
        {
            BuildForecastPeriods();
        }

        Recalculate();
    }

    private void BuildForecastPeriods()
    {
        ForecastPeriods.Clear();
        AddForecastPeriod(ThreeDayProjection);
        AddForecastPeriod(SevenDayProjection);
        AddForecastPeriod(FourteenDayProjection);
        AddForecastPeriod(ThirtyDayProjection);
    }

    private void AddForecastPeriod(int dayCount)
    {
        var option = new ForecastPeriodOptionViewModel(
            dayCount,
            AppResources.Format("Forecast_DayOption", dayCount),
            SetProjectionPeriod);
        option.SetSelected(dayCount == _projectionDayCount);
        ForecastPeriods.Add(option);
    }

    private void SetProjectionPeriod(int dayCount)
    {
        if (_projectionDayCount == dayCount)
        {
            return;
        }

        _projectionDayCount = dayCount;
        foreach (var period in ForecastPeriods)
        {
            period.SetSelected(period.DayCount == dayCount);
        }

        Recalculate();
    }

    private void Recalculate()
    {
        var subscriptions = _subscriptions;

        var visibleSubscriptions = subscriptions
            .Where(subscription => !subscription.IsArchived && !subscription.IsDeleted)
            .ToList();
        ActiveSubscriptionCount = visibleSubscriptions.Count(subscription =>
            subscription.LifecycleStatus is SubscriptionLifecycleStatus.Active or
                SubscriptionLifecycleStatus.Trial);
        ArchivedSubscriptionCount = subscriptions.Count(subscription => subscription.IsArchived);
        TrialSubscriptionCount = visibleSubscriptions.Count(subscription =>
            subscription.LifecycleStatus == SubscriptionLifecycleStatus.Trial);
        ExcludedSubscriptionCount = visibleSubscriptions.Count(subscription =>
            !subscription.ParticipatesInBudget);

        var projectionStartsOn = DateOnly.FromDateTime(DateTime.Today);
        var projectionEndsOn = projectionStartsOn.AddDays(_projectionDayCount - 1);
        var projection = _cashFlowProjector.Project(
            visibleSubscriptions,
            projectionStartsOn,
            projectionEndsOn);
        ProjectedPaymentCount = projection.Items.Count;

        ReplaceItems(
            CurrencyTotals,
            projection.CurrencyTotals.Select(CurrencyTotalViewModel.FromTotal));

        var upcomingPayments = projection.Items
            .Take(MaximumUpcomingPaymentCount)
            .Select(CashFlowItemViewModel.FromItem);
        ReplaceItems(UpcomingPayments, upcomingPayments);

        NextPaymentLabel = UpcomingPayments.FirstOrDefault()?.ScheduledOnLabel ??
            AppResources.Get("Common_NothingScheduled");
        OnPropertyChanged(nameof(ProjectionPeriodLabel));
        OnPropertyChanged(nameof(HasCurrencyTotals));
        OnPropertyChanged(nameof(HasNoCurrencyTotals));
        OnPropertyChanged(nameof(HasUpcomingPayments));
        OnPropertyChanged(nameof(HasNoUpcomingPayments));
        OnPropertyChanged(nameof(HasExcludedSubscriptions));
        OnPropertyChanged(nameof(ExcludedSubscriptionLabel));
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();

        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
