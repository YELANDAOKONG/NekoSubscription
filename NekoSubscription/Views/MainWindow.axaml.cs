using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Styling;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public partial class MainWindow : Window
{
    private const double DefaultWindowHeight = 820;
    private const double DefaultWindowWidth = 1280;
    private const double MinimumWindowHeight = 680;
    private const double MinimumWindowWidth = 980;
    private const double SidebarWidth = 224;

    private readonly ExperimentalAcrylicBorder _acrylicBorder;
    private readonly ExperimentalAcrylicMaterial _acrylicMaterial;
    private readonly Border _contentHost;
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        _viewModel = viewModel;
        DataContext = viewModel;
        Title = AppResources.Get("App_Name");
        Width = DefaultWindowWidth;
        Height = DefaultWindowHeight;
        MinWidth = MinimumWindowWidth;
        MinHeight = MinimumWindowHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        TransparencyBackgroundFallback = new SolidColorBrush(Color.FromRgb(244, 244, 248));

        _acrylicMaterial = new ExperimentalAcrylicMaterial
        {
            BackgroundSource = AcrylicBackgroundSource.Digger,
            TintColor = Colors.White,
            TintOpacity = 0.12,
            FallbackColor = Color.FromRgb(244, 244, 248),
            MaterialOpacity = ApplicationSettings.DefaultAcrylicOpacity
        };
        _acrylicBorder = new ExperimentalAcrylicBorder
        {
            IsHitTestVisible = false,
            Material = _acrylicMaterial
        };
        _contentHost = new Border
        {
            Child = BuildShell()
        };

        Content = new Grid()
            .Children(
                _acrylicBorder,
                _contentHost);

        _viewModel.AppearanceChanged += OnAppearanceChanged;
        _viewModel.LanguageChanged += OnLanguageChanged;
        ApplyAppearance();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.AppearanceChanged -= OnAppearanceChanged;
        _viewModel.LanguageChanged -= OnLanguageChanged;
        base.OnClosed(e);
    }

    private static Control BuildShell()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions($"{SidebarWidth},*"),
            RowDefinitions = new RowDefinitions("*,Auto")
        }
        .Children(
            BuildSidebar()
                .Grid_Column(0)
                .Grid_RowSpan(2),
            BuildWorkspace()
                .Grid_Column(1)
                .Grid_Row(0),
            BuildStatusBar()
                .Grid_Column(1)
                .Grid_Row(1));
    }

    private static Control BuildSidebar()
    {
        return new Border
        {
            Background = UiPalette.SidebarSurface,
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(16, 22),
            Child = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
                RowSpacing = 22
            }
            .Children(
                BuildBrand().Grid_Row(0),
                BuildNavigation().Grid_Row(1),
                BuildPrivacyNote().Grid_Row(3))
        };
    }

    private static Control BuildBrand()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 11
        }
        .Children(
            new Border
            {
                Width = 38,
                Height = 38,
                Background = UiPalette.Accent,
                CornerRadius = new CornerRadius(11),
                Child = new TextBlock
                {
                    Text = "N",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            },
            new StackPanel
            {
                Spacing = 1,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Children(
                new TextBlock
                {
                    Text = AppResources.Get("App_Name"),
                    FontSize = 15,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = AppResources.Get("Brand_LocalPlanner"),
                    FontSize = 9,
                    FontWeight = FontWeight.Bold,
                    Opacity = 0.5
                })
            .Grid_Column(1));
    }

    private static Control BuildNavigation()
    {
        return new StackPanel
        {
            Spacing = 6
        }
        .Children(
            BuildNavigationButton(
                AppResources.Get("Nav_Overview"),
                AppResources.Get("Nav_OverviewSubtitle"),
                "O",
                nameof(MainViewModel.IsDashboardSelected),
                nameof(MainViewModel.ShowDashboardCommand)),
            BuildNavigationButton(
                AppResources.Get("Nav_Calendar"),
                AppResources.Get("Nav_CalendarSubtitle"),
                "C",
                nameof(MainViewModel.IsCalendarSelected),
                nameof(MainViewModel.ShowCalendarCommand)),
            BuildNavigationButton(
                AppResources.Get("Nav_Subscriptions"),
                AppResources.Get("Nav_SubscriptionsSubtitle"),
                "S",
                nameof(MainViewModel.IsSubscriptionsSelected),
                nameof(MainViewModel.ShowSubscriptionsCommand)),
            BuildNavigationButton(
                AppResources.Get("Nav_Settings"),
                AppResources.Get("Nav_SettingsSubtitle"),
                "A",
                nameof(MainViewModel.IsSettingsSelected),
                nameof(MainViewModel.ShowSettingsCommand)));
    }

    private static Control BuildNavigationButton(
        string title,
        string subtitle,
        string glyph,
        string selectedPath,
        string commandPath)
    {
        var button = new ToggleButton
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(10, 9),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 10
            }
            .Children(
                new Border
                {
                    Width = 28,
                    Height = 28,
                    Background = UiPalette.AccentSurface,
                    CornerRadius = new CornerRadius(8),
                    Child = new TextBlock
                    {
                        Text = glyph,
                        FontSize = 12,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                },
                new StackPanel
                {
                    Spacing = 1,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Children(
                    new TextBlock
                    {
                        Text = title,
                        FontWeight = FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = subtitle,
                        FontSize = 10,
                        Opacity = 0.56
                    })
                .Grid_Column(1))
        };
        button.Bind(
            ToggleButton.IsCheckedProperty,
            new Binding(selectedPath)
            {
                Mode = BindingMode.OneWay
            });
        button.Bind(Button.CommandProperty, new Binding(commandPath));
        return button;
    }

    private static Control BuildPrivacyNote()
    {
        return new Border
        {
            Background = UiPalette.SuccessSurface,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(11),
            Child = new StackPanel
            {
                Spacing = 3
            }
            .Children(
                new TextBlock
                {
                    Text = AppResources.Get("Brand_LocalFirst"),
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold
                },
                new TextBlock
                {
                    Text = AppResources.Get("Brand_LocalDataStays"),
                    FontSize = 10,
                    Opacity = 0.62,
                    TextWrapping = TextWrapping.Wrap
                })
        };
    }

    private static Control BuildWorkspace()
    {
        var pageTitle = UiFactory.BoundText(
            nameof(MainViewModel.PageTitle),
            26,
            FontWeight.SemiBold);
        var pageSubtitle = UiFactory.BoundText(
            nameof(MainViewModel.PageSubtitle),
            13,
            opacity: 0.62,
            textWrapping: TextWrapping.Wrap);

        var pageContent = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        pageContent.Bind(
            ContentControl.ContentProperty,
            new Binding(nameof(MainViewModel.CurrentPage)));

        var progressBar = new ProgressBar
        {
            IsIndeterminate = true,
            Height = 2,
            Margin = new Thickness(-26, -20, -26, 0)
        };
        progressBar.Bind(
            IsVisibleProperty,
            new Binding(nameof(MainViewModel.IsBusy)));

        return new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 18,
            Margin = new Thickness(26, 20, 18, 0)
        }
        .Children(
            progressBar.Grid_Row(0),
            new StackPanel
            {
                Spacing = 3
            }
            .Children(pageTitle, pageSubtitle)
            .Grid_Row(1),
            pageContent.Grid_Row(2));
    }

    private static Control BuildStatusBar()
    {
        return new Border
        {
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(26, 9, 18, 10),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 8
            }
            .Children(
                new Border
                {
                    Width = 7,
                    Height = 7,
                    Background = UiPalette.Success,
                    CornerRadius = new CornerRadius(4),
                    VerticalAlignment = VerticalAlignment.Center
                },
                UiFactory.BoundText(
                        nameof(MainViewModel.StatusMessage),
                        11,
                        opacity: 0.62,
                        textWrapping: TextWrapping.Wrap)
                    .Grid_Column(1))
        };
    }

    private void OnAppearanceChanged(object? sender, EventArgs e) => ApplyAppearance();

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Title = AppResources.Get("App_Name");
        _contentHost.Child = BuildShell();
    }

    private void ApplyAppearance()
    {
        var settings = _viewModel.Settings;
        var themeVariant = settings.SelectedTheme switch
        {
            ApplicationTheme.System => ThemeVariant.Default,
            ApplicationTheme.Light => ThemeVariant.Light,
            ApplicationTheme.Dark => ThemeVariant.Dark,
            _ => throw new ArgumentOutOfRangeException(
                nameof(settings),
                settings.SelectedTheme,
                "The application theme is invalid.")
        };
        if (Application.Current is { } application)
        {
            application.RequestedThemeVariant = themeVariant;
        }

        var usesAcrylic = settings.SelectedVisualStyle == ApplicationVisualStyle.Acrylic;
        var usesDarkTheme = settings.SelectedTheme == ApplicationTheme.Dark ||
            (settings.SelectedTheme == ApplicationTheme.System && ActualThemeVariant == ThemeVariant.Dark);
        var standardBackground = new SolidColorBrush(
            usesDarkTheme ? Color.FromRgb(25, 25, 30) : Color.FromRgb(244, 244, 248));
        _acrylicMaterial.TintColor = usesDarkTheme ? Colors.Black : Colors.White;
        _acrylicMaterial.FallbackColor = standardBackground.Color;
        _acrylicMaterial.MaterialOpacity = settings.AcrylicOpacity;
        _acrylicBorder.IsVisible = usesAcrylic;
        TransparencyBackgroundFallback = standardBackground;
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
