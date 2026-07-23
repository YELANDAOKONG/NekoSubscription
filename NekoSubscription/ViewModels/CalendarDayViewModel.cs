using System;
using System.Collections.Generic;
using System.Linq;

using CommunityToolkit.Mvvm.Input;

using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public sealed class CalendarDayViewModel
{
    private const int MaximumVisiblePaymentCount = 2;

    public CalendarDayViewModel(
        DateOnly date,
        bool isInDisplayedMonth,
        bool isToday,
        bool isSelected,
        IReadOnlyList<CalendarPaymentViewModel> payments,
        Action<DateOnly> select)
    {
        ArgumentNullException.ThrowIfNull(payments);
        ArgumentNullException.ThrowIfNull(select);

        Date = date;
        DayNumberLabel = date.Day.ToString();
        IsInDisplayedMonth = isInDisplayedMonth;
        IsToday = isToday;
        IsSelected = isSelected;
        Payments = payments;
        VisiblePayments = payments.Take(MaximumVisiblePaymentCount).ToArray();
        AdditionalPaymentCount = Math.Max(0, payments.Count - MaximumVisiblePaymentCount);
        SelectCommand = new RelayCommand(() => select(date));
    }

    public int AdditionalPaymentCount { get; }

    public string AdditionalPaymentLabel => AppResources.Format(
        "Calendar_AdditionalPayments",
        AdditionalPaymentCount);

    public DateOnly Date { get; }

    public string DayNumberLabel { get; }

    public bool HasAdditionalPayments => AdditionalPaymentCount > 0;

    public bool IsInDisplayedMonth { get; }

    public bool IsSelected { get; }

    public bool IsToday { get; }

    public IReadOnlyList<CalendarPaymentViewModel> Payments { get; }

    public IRelayCommand SelectCommand { get; }

    public IReadOnlyList<CalendarPaymentViewModel> VisiblePayments { get; }
}
