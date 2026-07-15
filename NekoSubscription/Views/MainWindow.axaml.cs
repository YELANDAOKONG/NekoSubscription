using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;

using NekoSubscription.Core.Configuration;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public partial class MainWindow : Window
{
    private const double DefaultWindowHeight = 760;
    private const double DefaultWindowWidth = 1180;
    private const double MinimumWindowHeight = 600;
    private const double MinimumWindowWidth = 900;

    private static readonly IBrush CardBackground = new SolidColorBrush(Color.FromArgb(36, 127, 127, 127));
    private static readonly IBrush CardBorderBrush = new SolidColorBrush(Color.FromArgb(64, 127, 127, 127));

    private readonly ExperimentalAcrylicBorder _acrylicBorder;
    private readonly ExperimentalAcrylicMaterial _acrylicMaterial;
    private readonly Border _contentHost;
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        _viewModel = viewModel;
        DataContext = viewModel;
        Title = "NekoSubscription";
        Width = DefaultWindowWidth;
        Height = DefaultWindowHeight;
        MinWidth = MinimumWindowWidth;
        MinHeight = MinimumWindowHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = new WindowIcon(new Bitmap(AssetLoader.Open(
            new Uri("avares://NekoSubscription/Assets/avalonia-logo.ico"))));
        TransparencyBackgroundFallback = new SolidColorBrush(Color.FromRgb(242, 243, 245));

        _acrylicMaterial = new ExperimentalAcrylicMaterial
        {
            BackgroundSource = AcrylicBackgroundSource.Digger,
            TintColor = Colors.White,
            TintOpacity = 0.12,
            FallbackColor = Color.FromRgb(242, 243, 245),
            MaterialOpacity = ApplicationSettings.DefaultAcrylicOpacity
        };
        _acrylicBorder = new ExperimentalAcrylicBorder
        {
            IsHitTestVisible = false,
            Material = _acrylicMaterial
        };
        _contentHost = new Border
        {
            Child = BuildContent(viewModel)
        };

        Content = new Grid()
            .Children(
                _acrylicBorder,
                _contentHost);

        _viewModel.AppearanceChanged += OnAppearanceChanged;
        ApplyAppearance();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.AppearanceChanged -= OnAppearanceChanged;
        base.OnClosed(e);
    }

    private static Control BuildContent(MainViewModel viewModel)
    {
        return new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto,Auto"),
            Margin = new Thickness(24),
            RowSpacing = 16
        }
        .Children(
            BuildHeader(viewModel).Grid_Row(0),
            BuildCashFlowOverview(viewModel).Grid_Row(1),
            BuildSubscriptions(viewModel).Grid_Row(2),
            BuildAppearanceSettings(viewModel).Grid_Row(3),
            new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.75
            }
            .Text(viewModel, model => model.StatusMessage)
            .Grid_Row(4));
    }

    private static Control BuildHeader(MainViewModel viewModel)
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 16
        }
        .Children(
            new StackPanel
            {
                Spacing = 4
            }
            .Children(
                new TextBlock
                {
                    Text = "NekoSubscription",
                    FontSize = 28,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = "Offline subscription planning without currency conversion",
                    Opacity = 0.7
                }),
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Children(
                new CheckBox
                {
                    Content = "Show archived"
                }
                .IsChecked(viewModel, model => model.IncludeArchived, BindingMode.TwoWay),
                new Button
                {
                    Content = "Refresh",
                    MinWidth = 92
                }
                .Command(viewModel, model => model.RefreshCommand))
            .Grid_Column(1));
    }

    private static Control BuildCashFlowOverview(MainViewModel viewModel)
    {
        var totalsList = new ItemsControl
        {
            ItemTemplate = new FuncDataTemplate<CurrencyTotalViewModel>(
                (total, _) => BuildCurrencyTotal(total))
        }
        .ItemsSource(viewModel, model => model.CurrencyTotals);

        return CreateCard(
            new StackPanel
            {
                Spacing = 10
            }
            .Children(
                new TextBlock
                {
                    Text = "90-day cash flow by currency",
                    FontSize = 18,
                    FontWeight = FontWeight.SemiBold
                },
                totalsList));
    }

    private static Control BuildCurrencyTotal(CurrencyTotalViewModel? total)
    {
        if (total is null)
        {
            return new TextBlock { Text = "No total available" };
        }

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("180,*,*,*"),
            ColumnSpacing = 12,
            Margin = new Thickness(0, 3)
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
            new TextBlock { Text = total.TotalAmountLabel }.Grid_Column(3));
    }

    private static Control BuildSubscriptions(MainViewModel viewModel)
    {
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<SubscriptionListItemViewModel>(
                (subscription, _) => BuildSubscriptionRow(subscription)),
            SelectionMode = SelectionMode.Single
        }
        .ItemsSource(viewModel, model => model.Subscriptions)
        .SelectedItem(viewModel, model => model.SelectedSubscription, BindingMode.TwoWay)
        .Grid_Row(1);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        }
        .Children(
            new Button
            {
                MinWidth = 150,
                Content = new TextBlock().Text(viewModel, model => model.ArchiveActionLabel)
            }
            .Command(viewModel, model => model.ToggleArchiveCommand)
            .IsEnabled(viewModel, model => model.HasSelectedSubscription));

        return CreateCard(
            new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                RowSpacing = 10
            }
            .Children(
                new TextBlock
                {
                    Text = "Subscriptions",
                    FontSize = 18,
                    FontWeight = FontWeight.SemiBold
                }
                .Grid_Row(0),
                list,
                actions.Grid_Row(2)));
    }

    private static Control BuildSubscriptionRow(SubscriptionListItemViewModel? subscription)
    {
        if (subscription is null)
        {
            return new TextBlock { Text = "Subscription unavailable" };
        }

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2.2*,1.2*,110,130,110,110"),
            ColumnSpacing = 12,
            Margin = new Thickness(4, 8)
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
                    Text = subscription.AccountLabel,
                    Opacity = 0.7,
                    TextTrimming = TextTrimming.CharacterEllipsis
                })
            .Grid_Column(0),
            new TextBlock
            {
                Text = subscription.CategoryLabel,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Grid_Column(1),
            new TextBlock
            {
                Text = subscription.StatusLabel,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            }
            .Grid_Column(2),
            new TextBlock
            {
                Text = subscription.AmountLabel,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Grid_Column(3),
            new TextBlock
            {
                Text = subscription.NextBillingLabel,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Grid_Column(4),
            new TextBlock
            {
                Text = subscription.ImportanceLabel,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Grid_Column(5));
    }

    private static Control BuildAppearanceSettings(MainViewModel viewModel)
    {
        return CreateCard(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,180,Auto,220,70,Auto,*"),
                ColumnSpacing = 12
            }
            .Children(
                new TextBlock
                {
                    Text = "Appearance",
                    FontSize = 18,
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Grid_Column(0),
                new ComboBox
                {
                    ItemsSource = viewModel.VisualStyles,
                    MinWidth = 150
                }
                .SelectedItem(viewModel, model => model.SelectedVisualStyle, BindingMode.TwoWay)
                .Grid_Column(1),
                new TextBlock
                {
                    Text = "Acrylic opacity",
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Grid_Column(2),
                new Slider
                {
                    Minimum = ApplicationSettings.MinimumAcrylicOpacity,
                    Maximum = ApplicationSettings.MaximumAcrylicOpacity,
                    TickFrequency = 0.05,
                    IsSnapToTickEnabled = true,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Value(viewModel, model => model.AcrylicOpacity, BindingMode.TwoWay)
                .IsEnabled(viewModel, model => model.IsAcrylicSelected)
                .Grid_Column(3),
                new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Text(viewModel, model => model.AcrylicOpacityLabel)
                .Grid_Column(4),
                new Button
                {
                    Content = "Save appearance",
                    MinWidth = 130
                }
                .Command(viewModel, model => model.SaveAppearanceCommand)
                .Grid_Column(5)));
    }

    private static Border CreateCard(Control child)
    {
        return new Border
        {
            Background = CardBackground,
            BorderBrush = CardBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Child = child
        };
    }

    private void OnAppearanceChanged(object? sender, EventArgs e)
    {
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        var usesAcrylic = _viewModel.SelectedVisualStyle == ApplicationVisualStyle.Acrylic;
        var usesDarkTheme = ActualThemeVariant == ThemeVariant.Dark;
        var standardBackground = new SolidColorBrush(
            usesDarkTheme ? Color.FromRgb(31, 31, 31) : Color.FromRgb(242, 243, 245));
        _acrylicMaterial.TintColor = usesDarkTheme ? Colors.Black : Colors.White;
        _acrylicMaterial.FallbackColor = usesDarkTheme
            ? Color.FromRgb(31, 31, 31)
            : Color.FromRgb(242, 243, 245);
        TransparencyBackgroundFallback = standardBackground;
        _acrylicMaterial.MaterialOpacity = _viewModel.AcrylicOpacity;
        _acrylicBorder.IsVisible = usesAcrylic;
        Background = Brushes.Transparent;

        if (usesAcrylic)
        {
            TransparencyLevelHint =
                [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur];
            _contentHost.Background = Brushes.Transparent;
            return;
        }

        TransparencyLevelHint = [WindowTransparencyLevel.None];
        _contentHost.Background = standardBackground;
    }
}
