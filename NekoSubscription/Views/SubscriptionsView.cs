using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class SubscriptionsView : UserControl
{
    public SubscriptionsView()
    {
        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 16,
            Margin = new Thickness(0, 0, 8, 14)
        }
        .Children(
            BuildToolbar().Grid_Row(0),
            BuildWorkspace().Grid_Row(1));
    }

    private static Control BuildToolbar()
    {
        var add = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_Add"), AppIcons.Add);
        add.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.AddSubscriptionCommand)));

        var refresh = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            }
            .Children(
                UiFactory.Icon(AppIcons.Refresh, 14),
                new TextBlock { Text = AppResources.Get("Subscriptions_Refresh"), VerticalAlignment = VerticalAlignment.Center })
        };
        refresh.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.RefreshCommand)));

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 10,
            Margin = new Thickness(0, 0, 0, 16)
        }
        .Children(
            new StackPanel
            {
                Spacing = 4,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Children(
                UiFactory.SectionTitle(AppResources.Get("Subscriptions_All")),
                UiFactory.BoundText(nameof(SubscriptionsViewModel.ResultSummary), 12, opacity: 0.62)
            ),
            refresh.Grid_Column(1),
            add.Grid_Column(2));

        var searchBox = new TextBox
        {
            PlaceholderText = AppResources.Get("Subscriptions_SearchPlaceholder"),
            MinWidth = 260,
            CornerRadius = new CornerRadius(8),
            InnerLeftContent = new Border
            {
                Padding = new Thickness(10, 0, 6, 0),
                Child = UiFactory.Icon(AppIcons.Search, 14)
            }
        };
        searchBox.Bind(
            TextBox.TextProperty,
            new Binding(nameof(SubscriptionsViewModel.SearchText))
            {
                Mode = BindingMode.TwoWay
            });

        var categoryFilter = new ComboBox
        {
            MinWidth = 160,
            CornerRadius = new CornerRadius(8)
        };
        categoryFilter.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(SubscriptionsViewModel.CategoryFilters)));
        categoryFilter.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(SubscriptionsViewModel.SelectedCategoryFilter))
            {
                Mode = BindingMode.TwoWay
            });

        var includeArchived = new CheckBox
        {
            Content = AppResources.Get("Subscriptions_ShowArchived"),
            VerticalAlignment = VerticalAlignment.Center
        };
        includeArchived.Bind(
            Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding(nameof(SubscriptionsViewModel.IncludeArchived))
            {
                Mode = BindingMode.TwoWay
            });

        var filterRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"),
            ColumnSpacing = 12
        }
        .Children(
            searchBox.Grid_Column(0),
            categoryFilter.Grid_Column(1),
            new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Children(includeArchived)
            .Grid_Column(2));

        return UiFactory.Card(
            new StackPanel().Children(headerRow, filterRow),
            new Thickness(16));
    }

    private static Control BuildWorkspace()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1*,1.4*"),
            ColumnSpacing = 16
        }
        .Children(
            BuildSubscriptionList().Grid_Column(0),
            BuildRightPane().Grid_Column(1));
    }

    private static Control BuildSubscriptionList()
    {
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<SubscriptionListItemViewModel>(
                (subscription, _) => BuildSubscriptionRow(subscription)),
            SelectionMode = SelectionMode.Single,
            Background = Brushes.Transparent
        };
        list.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(nameof(SubscriptionsViewModel.Subscriptions)));
        list.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(SubscriptionsViewModel.SelectedSubscription))
            {
                Mode = BindingMode.TwoWay
            });

        var empty = UiFactory.EmptyState(
            AppResources.Get("Subscriptions_EmptyTitle"),
            AppResources.Get("Subscriptions_EmptyDescription"));
        empty.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasNoResults)));

        return UiFactory.Card(
            new Grid()
            .Children(list, empty),
            new Thickness(4));
    }

    private static Control BuildSubscriptionRow(SubscriptionListItemViewModel? subscription)
    {
        if (subscription is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_SubscriptionUnavailable") };
        }

        var serviceName = string.IsNullOrEmpty(subscription.ProviderLabel) ? subscription.ServiceLabel : $"{subscription.ProviderLabel} {subscription.ServiceLabel}";

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 14,
            Margin = new Thickness(4, 6)
        }
        .Children(
            BuildAvatar(subscription.ServiceLabel, 40, 18).Grid_Column(0),
            new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 2
            }
            .Children(
                new TextBlock
                {
                    Text = serviceName,
                    FontWeight = FontWeight.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    FontSize = 14
                },
                new TextBlock
                {
                    Text = subscription.NextBillingLabel,
                    FontSize = 11,
                    Opacity = 0.62,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            )
            .Grid_Column(1),
            new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 4
            }
            .Children(
                new TextBlock
                {
                    Text = subscription.AmountLabel,
                    FontWeight = FontWeight.Bold,
                    FontSize = 14,
                    TextAlignment = TextAlignment.Right
                },
                BuildStatus(subscription)
            )
            .Grid_Column(2));
    }

    private static Control BuildStatus(SubscriptionListItemViewModel subscription)
    {
        var background = subscription.IsArchived
            ? UiPalette.SurfaceStrong
            : subscription.LifecycleStatus switch
            {
                SubscriptionLifecycleStatus.Active => UiPalette.SuccessSurface,
                SubscriptionLifecycleStatus.Trial => UiPalette.WarningSurface,
                _ => UiPalette.AccentSurface
            };

        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(7, 2),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = new TextBlock
            {
                Text = subscription.IsArchived
                    ? AppResources.Get("Details_Archived")
                    : subscription.LifecycleLabel,
                FontSize = 9,
                FontWeight = FontWeight.Medium
            }
        };
    }

    private static Control BuildRightPane()
    {
        var editor = new ContentControl();
        editor.Bind(
            ContentControl.ContentProperty,
            new Binding(nameof(SubscriptionsViewModel.CurrentEditor)));
        editor.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasEditor)));

        var details = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = BuildDetails()
        };
        details.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasSelectedSubscriptionWithoutEditor)));

        var empty = UiFactory.EmptyState(
            AppResources.Get("Subscriptions_SelectTitle"),
            AppResources.Get("Subscriptions_SelectDescription"));
        empty.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasEmptyDetails)));

        return new Grid()
            .Children(editor, details, empty);
    }

    private static Control BuildDetails()
    {
        var edit = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_Edit"), AppIcons.Edit);
        edit.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.EditSubscriptionCommand)));

        var archiveText = new TextBlock();
        archiveText.Bind(TextBlock.TextProperty, new Binding(nameof(SubscriptionsViewModel.ArchiveActionLabel)));

        var archive = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            }
            .Children(
                UiFactory.Icon(AppIcons.Archive, 14),
                archiveText)
        };
        archive.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.ToggleArchiveCommand)));

        var delete = UiFactory.DangerButton(AppResources.Get("Subscriptions_Delete"), AppIcons.Delete);
        delete.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.RequestDeleteSubscriptionCommand)));

        var actionBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 24, 0, 0)
        }.Children(edit, archive, delete);

        var heroHeader = new StackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        }.Children(
            BuildBoundAvatar(SelectedPath(nameof(SubscriptionListItemViewModel.ServiceLabel)), 72, 32),
            new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center
            }
            .Children(
                UiFactory.BoundText(
                    SelectedPath(nameof(SubscriptionListItemViewModel.ServiceLabel)),
                    26,
                    FontWeight.Bold,
                    textAlignment: TextAlignment.Center),
                UiFactory.BoundText(
                    SelectedPath(nameof(SubscriptionListItemViewModel.ProviderLabel)),
                    14,
                    opacity: 0.62,
                    textAlignment: TextAlignment.Center)
            )
        );

        var highlightCards = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            ColumnSpacing = 16,
            Margin = new Thickness(0, 24, 0, 24)
        }.Children(
            BuildHighlightCard(
                AppResources.Get("Column_Amount"),
                SelectedPath(nameof(SubscriptionListItemViewModel.AmountLabel)),
                SelectedPath(nameof(SubscriptionListItemViewModel.BudgetStateLabel)),
                UiPalette.AccentSurface,
                UiPalette.Accent
            ).Grid_Column(0),
            BuildHighlightCard(
                AppResources.Get("Column_NextBilling"),
                SelectedPath(nameof(SubscriptionListItemViewModel.NextBillingLabel)),
                SelectedPath(nameof(SubscriptionListItemViewModel.ScheduleLabel)),
                UiPalette.SurfaceStrong,
                null
            ).Grid_Column(1)
        );

        var attributeGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto"),
            ColumnSpacing = 16,
            RowSpacing = 16
        }.Children(
            BuildAttributeCard(
                AppResources.Get("Column_Status"),
                SelectedPath(nameof(SubscriptionListItemViewModel.LifecycleLabel)),
                SelectedPath(nameof(SubscriptionListItemViewModel.StatusLabel))
            ).Grid_Column(0).Grid_Row(0),
            BuildAttributeCard(
                AppResources.Get("Subscriptions_Importance"),
                SelectedPath(nameof(SubscriptionListItemViewModel.ImportanceLabel)),
                SelectedPath(nameof(SubscriptionListItemViewModel.CategoryLabel))
            ).Grid_Column(1).Grid_Row(0),
            BuildAttributeCard(
                AppResources.Get("Editor_Account"),
                SelectedPath(nameof(SubscriptionListItemViewModel.AccountLabel)),
                SelectedPath(nameof(SubscriptionListItemViewModel.ArchiveStateLabel))
            ).Grid_Column(0).Grid_Row(1),
            BuildAttributeCard(
                AppResources.Get("Subscriptions_Details"),
                SelectedPath(nameof(SubscriptionListItemViewModel.SpecializedDetailsLabel)),
                SelectedPath(nameof(SubscriptionListItemViewModel.ManagementUrlLabel))
            ).Grid_Column(1).Grid_Row(1),
            BuildAttributeCard(
                AppResources.Get("Editor_Notes"),
                SelectedPath(nameof(SubscriptionListItemViewModel.NotesLabel)),
                null
            ).Grid_ColumnSpan(2).Grid_Row(2)
        );

        return new StackPanel
        {
            Margin = new Thickness(4, 0, 12, 12)
        }
        .Children(
            UiFactory.Card(
                new StackPanel().Children(
                    heroHeader,
                    highlightCards,
                    attributeGrid,
                    actionBar
                ),
                new Thickness(24)
            ),
            BuildDeleteConfirmation());
    }

    private static Control BuildHighlightCard(string title, string valuePath, string subtitlePath, IBrush background, IBrush? foreground)
    {
        var valueText = UiFactory.BoundText(valuePath, 24, FontWeight.Bold);
        if (foreground != null)
        {
            valueText.Foreground = foreground;
        }

        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16, 20),
            Child = new StackPanel
            {
                Spacing = 6,
                HorizontalAlignment = HorizontalAlignment.Center
            }
            .Children(
                new TextBlock
                {
                    Text = title,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Opacity = 0.6,
                    TextAlignment = TextAlignment.Center
                },
                valueText,
                UiFactory.BoundText(
                    subtitlePath,
                    12,
                    opacity: 0.68,
                    textAlignment: TextAlignment.Center)
            )
        };
    }

    private static Control BuildAttributeCard(string title, string primaryPath, string? secondaryPath)
    {
        var stack = new StackPanel { Spacing = 4 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Opacity = 0.5
        });
        stack.Children.Add(UiFactory.BoundText(
            primaryPath,
            14,
            FontWeight.SemiBold,
            textWrapping: TextWrapping.Wrap));
            
        if (secondaryPath != null)
        {
            stack.Children.Add(UiFactory.BoundText(
                secondaryPath,
                12,
                opacity: 0.6,
                textWrapping: TextWrapping.Wrap));
        }

        return new Border
        {
            Background = UiPalette.SurfaceStrong,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12),
            Child = stack
        };
    }

    private static Control BuildBoundAvatar(string propertyPath, double size, double fontSize)
    {
        var letterBlock = new TextBlock
        {
            Foreground = UiPalette.Accent,
            FontSize = fontSize,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        letterBlock.Bind(
            TextBlock.TextProperty,
            new Binding(propertyPath)
            {
                Converter = new FuncValueConverter<string?, string>(s => 
                    string.IsNullOrWhiteSpace(s) ? "?" : s.Substring(0, 1).ToUpperInvariant())
            });

        return new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = UiPalette.AccentSurface,
            Child = letterBlock,
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private static Control BuildAvatar(string name, double size, double fontSize)
    {
        var letter = string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
        return new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = UiPalette.AccentSurface,
            Child = new TextBlock
            {
                Text = letter,
                Foreground = UiPalette.Accent,
                FontSize = fontSize,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static Control BuildDeleteConfirmation()
    {
        var cancel = new Button
        {
            Content = AppResources.Get("Common_Cancel")
        };
        cancel.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.CancelDeleteSubscriptionCommand)));

        var confirm = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_ConfirmDelete"));
        confirm.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.ConfirmDeleteSubscriptionCommand)));

        var confirmation = new Border
        {
            Background = UiPalette.DangerSurface,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(13),
            Margin = new Thickness(0, 12, 0, 0),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                ColumnSpacing = 8
            }
            .Children(
                new TextBlock
                {
                    Text = AppResources.Get("Subscriptions_DeleteConfirmation"),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                },
                cancel.Grid_Column(1),
                confirm.Grid_Column(2))
        };
        confirmation.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.IsDeleteConfirmationVisible)));
        return confirmation;
    }

    private static string SelectedPath(string propertyName) =>
        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{propertyName}";
}
