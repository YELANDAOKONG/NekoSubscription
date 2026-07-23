using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class DashboardView : UserControl
{
    public DashboardView()
    {
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 14,
                Margin = new Thickness(0, 0, 8, 16)
            }
            .Children(
                BuildForecastHeader(),
                BuildExcludedNotice(),
                BuildMetrics(),
                BuildForecastWorkspace())
        };
    }

    private static Control BuildForecastHeader()
    {
        var periods = new ItemsControl
        {
            ItemsPanel = new FuncTemplate<Panel?>(() => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            }),
            ItemTemplate = new FuncDataTemplate<ForecastPeriodOptionViewModel>(
                (period, _) => BuildPeriodButton(period)),
            HorizontalAlignment = HorizontalAlignment.Right
        };
        periods.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(DashboardViewModel.ForecastPeriods)));

        return UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 18
            }
            .Children(
                new StackPanel
                {
                    Spacing = 3
                }
                .Children(
                    UiFactory.SectionTitle(AppResources.Get("Forecast_Title")),
                    UiFactory.BoundText(
                        nameof(DashboardViewModel.ProjectionPeriodLabel),
                        12,
                        opacity: 0.64,
                        textWrapping: TextWrapping.Wrap)),
                periods.Grid_Column(1)),
            new Thickness(16));
    }

    private static Control BuildPeriodButton(ForecastPeriodOptionViewModel? period)
    {
        if (period is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_Unknown") };
        }

        var button = new Avalonia.Controls.Primitives.ToggleButton
        {
            Content = period.Label,
            MinWidth = 64
        };
        button.Bind(
            Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding(nameof(ForecastPeriodOptionViewModel.IsSelected))
            {
                Mode = BindingMode.OneWay
            });
        button.Bind(
            Button.CommandProperty,
            new Binding(nameof(ForecastPeriodOptionViewModel.SelectCommand)));
        return button;
    }

    private static Control BuildExcludedNotice()
    {
        var notice = new Border
        {
            Background = UiPalette.WarningSurface,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 9),
            Child = UiFactory.BoundText(
                nameof(DashboardViewModel.ExcludedSubscriptionLabel),
                12,
                textWrapping: TextWrapping.Wrap)
        };
        notice.Bind(
            IsVisibleProperty,
            new Binding(nameof(DashboardViewModel.HasExcludedSubscriptions)));
        return notice;
    }

    private static Control BuildMetrics()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*,*"),
            ColumnSpacing = 12
        }
        .Children(
            BuildMetricCard(
                    AppResources.Get("Dashboard_MetricPayments"),
                    nameof(DashboardViewModel.ProjectedPaymentCount),
                    AppResources.Get("Dashboard_MetricPaymentsCaption"),
                    UiPalette.Accent)
                .Grid_Column(0),
            BuildMetricCard(
                    AppResources.Get("Dashboard_MetricActive"),
                    nameof(DashboardViewModel.ActiveSubscriptionCount),
                    AppResources.Get("Dashboard_MetricActiveCaption"),
                    UiPalette.Success)
                .Grid_Column(1),
            BuildMetricCard(
                    AppResources.Get("Dashboard_MetricTrials"),
                    nameof(DashboardViewModel.TrialSubscriptionCount),
                    AppResources.Get("Dashboard_MetricTrialsCaption"),
                    UiPalette.Warning)
                .Grid_Column(2));
    }

    private static Border BuildMetricCard(string title, string valuePath, string caption, IBrush accent)
    {
        return UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 12
            }
            .Children(
                new Border
                {
                    Width = 5,
                    Background = accent,
                    CornerRadius = new CornerRadius(3)
                },
                new StackPanel
                {
                    Spacing = 2
                }
                .Children(
                    UiFactory.BoundText(valuePath, 25, FontWeight.SemiBold),
                    new TextBlock
                    {
                        Text = title,
                        FontWeight = FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = caption,
                        FontSize = 11,
                        Opacity = 0.62,
                        TextWrapping = TextWrapping.Wrap
                    })
                .Grid_Column(1)),
            new Thickness(14));
    }

    private static Control BuildForecastWorkspace()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1.2*,*"),
            ColumnSpacing = 14
        }
        .Children(
            BuildCashFlowCard().Grid_Column(0),
            BuildUpcomingCard().Grid_Column(1));
    }

    private static Control BuildCashFlowCard()
    {
        var totals = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<CurrencyTotalViewModel>(
                (total, _) => BuildCurrencyTotal(total))
        };
        totals.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(DashboardViewModel.CurrencyTotals)));
        totals.Bind(IsVisibleProperty, new Binding(nameof(DashboardViewModel.HasCurrencyTotals)));

        var emptyState = UiFactory.EmptyState(
            AppResources.Get("Dashboard_EmptyCashTitle"),
            AppResources.Get("Dashboard_EmptyCashDescription"));
        emptyState.MinHeight = 190;
        emptyState.Bind(IsVisibleProperty, new Binding(nameof(DashboardViewModel.HasNoCurrencyTotals)));

        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 14
            }
            .Children(
                BuildSectionHeader(
                    AppResources.Get("Forecast_CashFlowTitle"),
                    AppResources.Get("Dashboard_CashFlowDescription")),
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("1.35*,*,*,*"),
                    ColumnSpacing = 10,
                    Margin = new Thickness(10, 0)
                }
                .Children(
                    BuildColumnHeader(AppResources.Get("Column_Currency")).Grid_Column(0),
                    BuildColumnHeader(AppResources.Get("Column_Fixed")).Grid_Column(1),
                    BuildColumnHeader(AppResources.Get("Column_Estimated")).Grid_Column(2),
                    BuildColumnHeader(AppResources.Get("Column_Total")).Grid_Column(3)),
                totals,
                emptyState));
    }

    private static Control BuildUpcomingCard()
    {
        var list = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<CashFlowItemViewModel>(
                (payment, _) => BuildUpcomingRow(payment))
        };
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(DashboardViewModel.UpcomingPayments)));
        list.Bind(IsVisibleProperty, new Binding(nameof(DashboardViewModel.HasUpcomingPayments)));

        var emptyState = UiFactory.EmptyState(
            AppResources.Get("Dashboard_EmptyUpcomingTitle"),
            AppResources.Get("Dashboard_EmptyUpcomingDescription"));
        emptyState.MinHeight = 190;
        emptyState.Bind(IsVisibleProperty, new Binding(nameof(DashboardViewModel.HasNoUpcomingPayments)));

        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 12
            }
            .Children(
                BuildSectionHeader(
                    AppResources.Get("Dashboard_UpcomingTitle"),
                    AppResources.Get("Forecast_UpcomingDescription")),
                list,
                emptyState));
    }

    private static Control BuildSectionHeader(string title, string description)
    {
        return new StackPanel
        {
            Spacing = 3
        }
        .Children(
            UiFactory.SectionTitle(title),
            new TextBlock
            {
                Text = description,
                FontSize = 12,
                Opacity = 0.64,
                TextWrapping = TextWrapping.Wrap
            });
    }

    private static Control BuildCurrencyTotal(CurrencyTotalViewModel? total)
    {
        if (total is null)
        {
            return new TextBlock { Text = AppResources.Get("Dashboard_CurrencyUnavailable") };
        }

        return new Border
        {
            Background = UiPalette.SurfaceStrong,
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 9),
            Margin = new Thickness(0, 3),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("1.35*,*,*,*"),
                ColumnSpacing = 10
            }
            .Children(
                new TextBlock
                {
                    Text = total.CurrencyLabel,
                    FontWeight = FontWeight.SemiBold
                }
                .Grid_Column(0),
                new TextBlock { Text = total.FixedAmountLabel }.Grid_Column(1),
                new TextBlock { Text = total.EstimatedAmountLabel }.Grid_Column(2),
                new TextBlock
                {
                    Text = total.TotalAmountLabel,
                    FontWeight = FontWeight.SemiBold
                }
                .Grid_Column(3))
        };
    }

    private static Control BuildUpcomingRow(CashFlowItemViewModel? payment)
    {
        if (payment is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_SubscriptionUnavailable") };
        }

        return new Border
        {
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(4, 10),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 12
            }
            .Children(
                new StackPanel
                {
                    Spacing = 2
                }
                .Children(
                    new TextBlock
                    {
                        Text = payment.ServiceLabel,
                        FontWeight = FontWeight.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    },
                    new TextBlock
                    {
                        Text = $"{payment.ScheduledOnLabel} · {payment.AmountKindLabel}",
                        FontSize = 11,
                        Opacity = 0.62
                    }),
                new TextBlock
                {
                    Text = payment.AmountLabel,
                    FontWeight = FontWeight.Medium,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Grid_Column(1))
        };
    }

    private static TextBlock BuildColumnHeader(string text)
    {
        return new TextBlock
        {
            Text = text.ToUpperInvariant(),
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Opacity = 0.58
        };
    }
}
