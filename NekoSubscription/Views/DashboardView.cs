using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

using NekoSubscription.ViewModels;
using NekoSubscription.Localization;

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
                Spacing = 16,
                Margin = new Thickness(0, 0, 8, 8)
            }
            .Children(
                BuildWelcomeCard(),
                BuildMetrics(),
                BuildCashFlowCard(),
                BuildUpcomingCard())
        };
    }

    private static Control BuildWelcomeCard()
    {
        return new Border
        {
            Background = UiPalette.AccentSurface,
            BorderBrush = UiPalette.Accent,
            BorderThickness = new Thickness(1, 1, 4, 1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(22),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 24
            }
            .Children(
                new StackPanel
                {
                    Spacing = 6
                }
                .Children(
                    new TextBlock
                    {
                        Text = AppResources.Get("Dashboard_Eyebrow"),
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                        Foreground = UiPalette.Accent
                    },
                    new TextBlock
                    {
                        Text = AppResources.Get("Dashboard_HeroTitle"),
                        FontSize = 22,
                        FontWeight = FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = AppResources.Get("Dashboard_HeroDescription"),
                        Opacity = 0.72,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 680
                    }),
                new StackPanel
                {
                    Spacing = 3,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Children(
                    new TextBlock
                    {
                        Text = AppResources.Get("Dashboard_NextPayment"),
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                        Opacity = 0.62
                    },
                    UiFactory.BoundText(
                        nameof(DashboardViewModel.NextPaymentLabel),
                        18,
                        FontWeight.SemiBold))
                .Grid_Column(1))
        };
    }

    private static Control BuildMetrics()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 12,
            RowSpacing = 12
        }
        .Children(
            BuildMetricCard(
                    AppResources.Get("Dashboard_MetricActive"),
                    nameof(DashboardViewModel.ActiveSubscriptionCount),
                    AppResources.Get("Dashboard_MetricActiveCaption"),
                    UiPalette.Success)
                .Grid_Column(0),
            BuildMetricCard(
                    AppResources.Get("Dashboard_MetricTrials"),
                    nameof(DashboardViewModel.TrialSubscriptionCount),
                    AppResources.Get("Dashboard_MetricTrialsCaption"),
                    UiPalette.Warning)
                .Grid_Column(1),
            BuildMetricCard(
                    AppResources.Get("Dashboard_MetricPayments"),
                    nameof(DashboardViewModel.ProjectedPaymentCount),
                    AppResources.Get("Dashboard_MetricPaymentsCaption"),
                    UiPalette.Accent)
                .Grid_Column(0)
                .Grid_Row(1),
            BuildMetricCard(
                    AppResources.Get("Dashboard_MetricArchived"),
                    nameof(DashboardViewModel.ArchivedSubscriptionCount),
                    AppResources.Get("Dashboard_MetricArchivedCaption"),
                    UiPalette.Border)
                .Grid_Column(1)
                .Grid_Row(1));
    }

    private static Border BuildMetricCard(string title, string valuePath, string caption, IBrush accent)
    {
        return UiFactory.Card(
            new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
                RowSpacing = 6
            }
            .Children(
                new Border
                {
                    Width = 28,
                    Height = 4,
                    Background = accent,
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Left
                }
                .Grid_Row(0),
                UiFactory.BoundText(valuePath, 26, FontWeight.SemiBold)
                    .Grid_Row(1),
                new StackPanel
                {
                    Spacing = 2
                }
                .Children(
                    new TextBlock
                    {
                        Text = title,
                        FontWeight = FontWeight.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = caption,
                        FontSize = 12,
                        Opacity = 0.62,
                        TextWrapping = TextWrapping.Wrap
                    })
                .Grid_Row(2)));
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
        emptyState.MinHeight = 130;
        emptyState.Bind(IsVisibleProperty, new Binding(nameof(DashboardViewModel.HasNoCurrencyTotals)));

        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 14
            }
            .Children(
                BuildSectionHeader(
                    AppResources.Get("Dashboard_CashFlowTitle"),
                    AppResources.Get("Dashboard_CashFlowDescription")),
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("1.4*,*,*,*"),
                    ColumnSpacing = 12,
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
            ItemTemplate = new FuncDataTemplate<SubscriptionListItemViewModel>(
                (subscription, _) => BuildUpcomingRow(subscription))
        };
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(DashboardViewModel.UpcomingSubscriptions)));
        list.Bind(IsVisibleProperty, new Binding(nameof(DashboardViewModel.HasUpcomingSubscriptions)));

        var emptyState = UiFactory.EmptyState(
            AppResources.Get("Dashboard_EmptyUpcomingTitle"),
            AppResources.Get("Dashboard_EmptyUpcomingDescription"));
        emptyState.MinHeight = 130;
        emptyState.Bind(IsVisibleProperty, new Binding(nameof(DashboardViewModel.HasNoUpcomingSubscriptions)));

        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 12
            }
            .Children(
                BuildSectionHeader(
                    AppResources.Get("Dashboard_UpcomingTitle"),
                    AppResources.Get("Dashboard_UpcomingDescription")),
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
                ColumnDefinitions = new ColumnDefinitions("1.4*,*,*,*"),
                ColumnSpacing = 12
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

    private static Control BuildUpcomingRow(SubscriptionListItemViewModel? subscription)
    {
        if (subscription is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_SubscriptionUnavailable") };
        }

        return new Border
        {
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(4, 11),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("2*,1.2*,Auto"),
                ColumnSpacing = 14
            }
            .Children(
                new StackPanel
                {
                    Spacing = 2
                }
                .Children(
                    new TextBlock
                    {
                        Text = subscription.ServiceLabel,
                        FontWeight = FontWeight.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    },
                    new TextBlock
                    {
                        Text = subscription.ProviderLabel,
                        FontSize = 12,
                        Opacity = 0.62
                    }),
                new TextBlock
                {
                    Text = subscription.AmountLabel,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Grid_Column(1),
                new TextBlock
                {
                    Text = subscription.NextBillingLabel,
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Grid_Column(2))
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
