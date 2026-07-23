using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class CalendarView : UserControl
{
    private const double CalendarDayMinimumHeight = 88;

    public CalendarView()
    {
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
                RowSpacing = 10,
                Margin = new Thickness(0, 0, 8, 14)
            }
            .Children(
                BuildToolbar().Grid_Row(0),
                BuildWeekdayHeader().Grid_Row(1),
                BuildCalendar().Grid_Row(2),
                BuildSelectedDay().Grid_Row(3))
        };
    }

    private static Control BuildToolbar()
    {
        var previous = new Button
        {
            Content = "‹",
            FontSize = 20,
            MinWidth = 42
        };
        previous.Bind(
            Button.CommandProperty,
            new Binding(nameof(CalendarViewModel.GoToPreviousMonthCommand)));

        var next = new Button
        {
            Content = "›",
            FontSize = 20,
            MinWidth = 42
        };
        next.Bind(
            Button.CommandProperty,
            new Binding(nameof(CalendarViewModel.GoToNextMonthCommand)));

        var today = new Button
        {
            Content = AppResources.Get("Calendar_Today"),
            MinWidth = 76
        };
        today.Bind(
            Button.CommandProperty,
            new Binding(nameof(CalendarViewModel.GoToTodayCommand)));

        var monthLabel = UiFactory.BoundText(
            nameof(CalendarViewModel.MonthLabel),
            20,
            FontWeight.SemiBold);
        monthLabel.VerticalAlignment = VerticalAlignment.Center;

        return UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
                ColumnSpacing = 8
            }
            .Children(
                previous,
                monthLabel.Grid_Column(1),
                today.Grid_Column(2),
                next.Grid_Column(3)),
            new Thickness(12));
    }

    private static Control BuildWeekdayHeader()
    {
        var labels = new[]
        {
            "Calendar_MondayShort",
            "Calendar_TuesdayShort",
            "Calendar_WednesdayShort",
            "Calendar_ThursdayShort",
            "Calendar_FridayShort",
            "Calendar_SaturdayShort",
            "Calendar_SundayShort"
        };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*")
        };

        for (var index = 0; index < labels.Length; index++)
        {
            grid.Children.Add(new TextBlock
            {
                Text = AppResources.Get(labels[index]),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Opacity = 0.58,
                TextAlignment = TextAlignment.Center
            }
            .Grid_Column(index));
        }

        return grid;
    }

    private static Control BuildCalendar()
    {
        var calendar = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new UniformGrid
            {
                Columns = 7,
                Rows = 6
            }),
            ItemTemplate = new FuncDataTemplate<CalendarDayViewModel>(
                (day, _) => BuildDay(day))
        };
        calendar.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(CalendarViewModel.Days)));
        return calendar;
    }

    private static Control BuildDay(CalendarDayViewModel? day)
    {
        if (day is null)
        {
            return new Border();
        }

        var content = new List<Control>
        {
            BuildDayNumber(day)
        };
        content.AddRange(day.VisiblePayments.Select(BuildDayPayment));
        if (day.HasAdditionalPayments)
        {
            content.Add(new TextBlock
            {
                Text = day.AdditionalPaymentLabel,
                FontSize = 10,
                Opacity = 0.64,
                Margin = new Thickness(3, 2, 3, 0)
            });
        }

        var button = new Button
        {
            Background = day.IsSelected ? UiPalette.AccentSurface : Brushes.Transparent,
            BorderBrush = day.IsSelected || day.IsToday ? UiPalette.Accent : UiPalette.Border,
            BorderThickness = new Thickness(day.IsSelected ? 2 : 1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6),
            Margin = new Thickness(2),
            MinHeight = CalendarDayMinimumHeight,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            Opacity = day.IsInDisplayedMonth ? 1 : 0.44,
            Content = new StackPanel
            {
                Spacing = 3
            }
            .Children(content.ToArray())
        };
        button.Bind(
            Button.CommandProperty,
            new Binding(nameof(CalendarDayViewModel.SelectCommand)));
        return button;
    }

    private static Control BuildDayNumber(CalendarDayViewModel day)
    {
        return new TextBlock
        {
            Text = day.DayNumberLabel,
            FontWeight = day.IsToday ? FontWeight.Bold : FontWeight.Medium,
            Foreground = day.IsToday ? UiPalette.Accent : null,
            Margin = new Thickness(2, 0, 2, 2)
        };
    }

    private static Control BuildDayPayment(CalendarPaymentViewModel payment)
    {
        return new Border
        {
            Background = payment.Item.IsEstimate
                ? UiPalette.WarningSurface
                : UiPalette.SuccessSurface,
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(5, 3),
            Child = new TextBlock
            {
                Text = payment.Item.IsEstimate
                    ? AppResources.Format(
                        "Calendar_EstimatedPaymentCompact",
                        payment.Item.ServiceLabel,
                        payment.Item.AmountLabel)
                    : $"{payment.Item.ServiceLabel} · {payment.Item.AmountLabel}",
                FontSize = 10,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };
    }

    private static Control BuildSelectedDay()
    {
        var payments = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<CalendarPaymentViewModel>(
                (payment, _) => BuildSelectedPayment(payment))
        };
        payments.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(CalendarViewModel.SelectedPayments)));
        payments.Bind(
            IsVisibleProperty,
            new Binding(nameof(CalendarViewModel.HasSelectedPayments)));

        var empty = new TextBlock
        {
            Text = AppResources.Get("Calendar_NoPayments"),
            Opacity = 0.62,
            Margin = new Thickness(0, 8, 0, 0)
        };
        empty.Bind(
            IsVisibleProperty,
            new Binding(nameof(CalendarViewModel.HasNoSelectedPayments)));

        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 6
            }
            .Children(
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto")
                }
                .Children(
                    UiFactory.BoundText(
                        nameof(CalendarViewModel.SelectedDateLabel),
                        16,
                        FontWeight.SemiBold),
                    UiFactory.BoundText(
                            nameof(CalendarViewModel.SelectedDaySummary),
                            11,
                            opacity: 0.62)
                        .Grid_Column(1)),
                payments,
                empty),
            new Thickness(14));
    }

    private static Control BuildSelectedPayment(CalendarPaymentViewModel? payment)
    {
        if (payment is null)
        {
            return new Border();
        }

        var open = new Button
        {
            Content = AppResources.Get("Calendar_ViewSubscription")
        };
        open.Bind(
            Button.CommandProperty,
            new Binding(nameof(CalendarPaymentViewModel.OpenSubscriptionCommand)));

        return new Border
        {
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(0, 8, 0, 0),
            Margin = new Thickness(0, 3, 0, 0),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                ColumnSpacing = 12
            }
            .Children(
                new StackPanel
                {
                    Spacing = 1
                }
                .Children(
                    new TextBlock
                    {
                        Text = payment.Item.ServiceLabel,
                        FontWeight = FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = $"{payment.Item.ProviderLabel} · {payment.Item.AmountKindLabel}",
                        FontSize = 11,
                        Opacity = 0.62
                    }),
                new TextBlock
                {
                    Text = payment.Item.AmountLabel,
                    FontWeight = FontWeight.Medium,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Grid_Column(1),
                open.Grid_Column(2))
        };
    }
}
