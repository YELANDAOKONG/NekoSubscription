using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class SettingsView : UserControl
{
    public SettingsView()
    {
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 16,
                Margin = new Thickness(0, 0, 8, 8),
                MaxWidth = 920,
                HorizontalAlignment = HorizontalAlignment.Stretch
            }
            .Children(
                BuildLanguageCard(),
                BuildAppearanceCard(),
                BuildMaterialCard(),
                BuildPrivacyCard(),
                BuildSaveBar())
        };
    }

    private static Control BuildLanguageCard()
    {
        var languageSelector = new ComboBox
        {
            MinWidth = 190
        };
        languageSelector.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(SettingsViewModel.Languages)));
        languageSelector.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(SettingsViewModel.SelectedLanguageOption))
            {
                Mode = BindingMode.TwoWay
            });

        return BuildSettingsCard(
            AppResources.Get("Settings_LanguageTitle"),
            AppResources.Get("Settings_LanguageDescription"),
            languageSelector);
    }

    private static Control BuildAppearanceCard()
    {
        var themeSelector = new ComboBox
        {
            MinWidth = 190
        };
        themeSelector.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(SettingsViewModel.Themes)));
        themeSelector.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(SettingsViewModel.SelectedThemeOption))
            {
                Mode = BindingMode.TwoWay
            });

        return BuildSettingsCard(
            AppResources.Get("Settings_ColorThemeTitle"),
            AppResources.Get("Settings_ColorThemeDescription"),
            themeSelector);
    }

    private static Control BuildMaterialCard()
    {
        var styleSelector = new ComboBox
        {
            MinWidth = 190
        };
        styleSelector.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(SettingsViewModel.VisualStyles)));
        styleSelector.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(SettingsViewModel.SelectedVisualStyleOption))
            {
                Mode = BindingMode.TwoWay
            });

        var opacitySlider = new Slider
        {
            Minimum = ApplicationSettings.MinimumAcrylicOpacity,
            Maximum = ApplicationSettings.MaximumAcrylicOpacity,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = true,
            MinWidth = 220,
            VerticalAlignment = VerticalAlignment.Center
        };
        opacitySlider.Bind(
            Avalonia.Controls.Primitives.RangeBase.ValueProperty,
            new Binding(nameof(SettingsViewModel.AcrylicOpacity))
            {
                Mode = BindingMode.TwoWay
            });
        opacitySlider.Bind(
            IsEnabledProperty,
            new Binding(nameof(SettingsViewModel.IsAcrylicSelected)));

        var opacityLabel = UiFactory.BoundText(
            nameof(SettingsViewModel.AcrylicOpacityLabel),
            13,
            FontWeight.SemiBold);

        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 16
            }
            .Children(
                BuildSettingsHeading(
                    AppResources.Get("Settings_WindowMaterialTitle"),
                    AppResources.Get("Settings_WindowMaterialDescription")),
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("180,*,Auto"),
                    ColumnSpacing = 16
                }
                .Children(
                    new TextBlock
                    {
                        Text = AppResources.Get("Settings_VisualStyle"),
                        FontWeight = FontWeight.Medium,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    styleSelector.Grid_Column(1)),
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("180,*,56"),
                    ColumnSpacing = 16
                }
                .Children(
                    new TextBlock
                    {
                        Text = AppResources.Get("Settings_AcrylicOpacity"),
                        FontWeight = FontWeight.Medium,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    opacitySlider.Grid_Column(1),
                    opacityLabel
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Grid_Column(2))));
    }

    private static Control BuildPrivacyCard()
    {
        return UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 16
            }
            .Children(
                new Border
                {
                    Width = 46,
                    Height = 46,
                    Background = UiPalette.SuccessSurface,
                    CornerRadius = new CornerRadius(23),
                    Child = new TextBlock
                    {
                        Text = "✓",
                        Foreground = UiPalette.Success,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                },
                new StackPanel
                {
                    Spacing = 4
                }
                .Children(
                    new TextBlock
                    {
                        Text = AppResources.Get("Settings_LocalFirstTitle"),
                        FontSize = 16,
                        FontWeight = FontWeight.SemiBold
                    },
                    new TextBlock
                    {
                        Text = AppResources.Get("Settings_LocalFirstDescription"),
                        Opacity = 0.68,
                        TextWrapping = TextWrapping.Wrap
                    })
                .Grid_Column(1)));
    }

    private static Control BuildSaveBar()
    {
        var saveButton = UiFactory.PrimaryButton(AppResources.Get("Settings_SaveChanges"));
        saveButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SettingsViewModel.SaveCommand)));
        saveButton.Bind(
            IsEnabledProperty,
            new Binding(nameof(SettingsViewModel.HasUnsavedChanges)));

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(2, 4)
        }
        .Children(
            new StackPanel
            {
                Spacing = 2,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Children(
                new TextBlock
                {
                    Text = AppResources.Get("Settings_ChangesPreview"),
                    FontWeight = FontWeight.Medium
                },
                new TextBlock
                {
                    Text = AppResources.Get("Settings_SaveDescription"),
                    FontSize = 12,
                    Opacity = 0.62
                }),
            saveButton.Grid_Column(1));
    }

    private static Control BuildSettingsCard(string title, string description, Control editor)
    {
        return UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                ColumnSpacing = 24
            }
            .Children(
                BuildSettingsHeading(title, description),
                editor
                    .VerticalAlignment(VerticalAlignment.Center)
                    .Grid_Column(1)));
    }

    private static Control BuildSettingsHeading(string title, string description)
    {
        return new StackPanel
        {
            Spacing = 4
        }
        .Children(
            new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeight.SemiBold
            },
            new TextBlock
            {
                Text = description,
                Opacity = 0.66,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 560
            });
    }
}
