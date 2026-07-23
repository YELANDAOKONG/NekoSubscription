using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public partial class CalendarViewModel : ViewModelBase
{
    private const int CalendarDayCount = 42;
    private const int DaysPerWeek = 7;

    private readonly CashFlowProjector _cashFlowProjector;
    private IReadOnlyList<Subscription> _subscriptions = [];

    public CalendarViewModel(CashFlowProjector cashFlowProjector)
    {
        ArgumentNullException.ThrowIfNull(cashFlowProjector);

        _cashFlowProjector = cashFlowProjector;
        var today = DateOnly.FromDateTime(DateTime.Today);
        DisplayedMonth = new DateOnly(today.Year, today.Month, 1);
        SelectedDate = today;
        RebuildCalendar();
    }

    public event Action<Guid>? SubscriptionRequested;

    public ObservableCollection<CalendarDayViewModel> Days { get; } = [];

    public ObservableCollection<CalendarPaymentViewModel> SelectedPayments { get; } = [];

    public bool HasSelectedPayments => SelectedPayments.Count > 0;

    public bool HasNoSelectedPayments => !HasSelectedPayments;

    public string MonthLabel => DisplayedMonth
        .ToDateTime(TimeOnly.MinValue)
        .ToString("Y", CultureInfo.CurrentCulture);

    public string SelectedDateLabel => SelectedDate
        .ToString("D", CultureInfo.CurrentCulture);

    public string SelectedDaySummary => AppResources.Format(
        "Calendar_SelectedDaySummary",
        SelectedPayments.Count);

    [ObservableProperty]
    public partial DateOnly DisplayedMonth { get; private set; }

    [ObservableProperty]
    public partial DateOnly SelectedDate { get; private set; }

    public void RefreshLocalization() => RebuildCalendar();

    public void Update(IReadOnlyList<Subscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        _subscriptions = subscriptions;
        RebuildCalendar();
    }

    [RelayCommand]
    private void GoToNextMonth() => MoveMonth(1);

    [RelayCommand]
    private void GoToPreviousMonth() => MoveMonth(-1);

    [RelayCommand]
    private void GoToToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        DisplayedMonth = new DateOnly(today.Year, today.Month, 1);
        SelectedDate = today;
        RebuildCalendar();
    }

    private void MoveMonth(int monthCount)
    {
        DisplayedMonth = DisplayedMonth.AddMonths(monthCount);
        SelectedDate = DisplayedMonth;
        RebuildCalendar();
    }

    private void OpenSubscription(Guid subscriptionId) =>
        SubscriptionRequested?.Invoke(subscriptionId);

    private void RebuildCalendar()
    {
        var startOffset = ((int)DisplayedMonth.DayOfWeek + DaysPerWeek - 1) % DaysPerWeek;
        var calendarStartsOn = DisplayedMonth.AddDays(-startOffset);
        var calendarEndsOn = calendarStartsOn.AddDays(CalendarDayCount - 1);
        var visibleSubscriptions = _subscriptions.Where(subscription =>
            !subscription.IsArchived && !subscription.IsDeleted);
        var projection = _cashFlowProjector.Project(
            visibleSubscriptions,
            calendarStartsOn,
            calendarEndsOn);
        var paymentsByDate = projection.Items
            .Select(CashFlowItemViewModel.FromItem)
            .Select(item => new CalendarPaymentViewModel(item, OpenSubscription))
            .GroupBy(payment => payment.Item.ScheduledOn)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<CalendarPaymentViewModel>)group.ToArray());
        var today = DateOnly.FromDateTime(DateTime.Today);

        Days.Clear();
        for (var dayOffset = 0; dayOffset < CalendarDayCount; dayOffset++)
        {
            var date = calendarStartsOn.AddDays(dayOffset);
            var payments = paymentsByDate.GetValueOrDefault(date) ?? [];
            Days.Add(new CalendarDayViewModel(
                date,
                date.Month == DisplayedMonth.Month && date.Year == DisplayedMonth.Year,
                date == today,
                date == SelectedDate,
                payments,
                SelectDate));
        }

        ReplaceSelectedPayments(paymentsByDate.GetValueOrDefault(SelectedDate) ?? []);
        OnPropertyChanged(nameof(MonthLabel));
        OnPropertyChanged(nameof(SelectedDateLabel));
        OnPropertyChanged(nameof(SelectedDaySummary));
        OnPropertyChanged(nameof(HasSelectedPayments));
        OnPropertyChanged(nameof(HasNoSelectedPayments));
    }

    private void SelectDate(DateOnly date)
    {
        SelectedDate = date;
        if (date.Month != DisplayedMonth.Month || date.Year != DisplayedMonth.Year)
        {
            DisplayedMonth = new DateOnly(date.Year, date.Month, 1);
        }

        RebuildCalendar();
    }

    private void ReplaceSelectedPayments(IEnumerable<CalendarPaymentViewModel> payments)
    {
        SelectedPayments.Clear();
        foreach (var payment in payments)
        {
            SelectedPayments.Add(payment);
        }
    }
}
