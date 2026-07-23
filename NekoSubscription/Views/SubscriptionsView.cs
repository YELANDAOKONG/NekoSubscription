using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
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
            RowSpacing = 12,
            Margin = new Thickness(0, 0, 8, 14)
        }
        .Children(
            BuildToolbar().Grid_Row(0),
            BuildWorkspace().Grid_Row(1));
    }

    private static Control BuildToolbar()
    {
        var searchBox = new TextBox
        {
            PlaceholderText = AppResources.Get("Subscriptions_SearchPlaceholder"),
            MinWidth = 240
        };
        searchBox.Bind(
            TextBox.TextProperty,
            new Binding(nameof(SubscriptionsViewModel.SearchText))
            {
                Mode = BindingMode.TwoWay
            });

        var categoryFilter = new ComboBox
        {
            MinWidth = 150
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

        var refresh = new Button
        {
            Content = AppResources.Get("Subscriptions_Refresh")
        };
        refresh.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.RefreshCommand)));

        var add = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_Add"));
        add.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.AddSubscriptionCommand)));

        return UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto,Auto"),
                ColumnSpacing = 9
            }
            .Children(
                searchBox,
                categoryFilter.Grid_Column(1),
                includeArchived.Grid_Column(2),
                refresh.Grid_Column(3),
                add.Grid_Column(4)),
            new Thickness(11));
    }

    private static Control BuildWorkspace()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("0.9*,1.15*"),
            ColumnSpacing = 12
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
            SelectionMode = SelectionMode.Single
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
            new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*"),
                RowSpacing = 10
            }
            .Children(
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto")
                }
                .Children(
                    UiFactory.SectionTitle(AppResources.Get("Subscriptions_All")),
                    UiFactory.BoundText(
                            nameof(SubscriptionsViewModel.ResultSummary),
                            11,
                            opacity: 0.62)
                        .Grid_Column(1)),
                new Grid()
                    .Children(list, empty)
                    .Grid_Row(1)),
            new Thickness(13));
    }

    private static Control BuildSubscriptionRow(SubscriptionListItemViewModel? subscription)
    {
        if (subscription is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_SubscriptionUnavailable") };
        }

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            ColumnSpacing = 10,
            RowSpacing = 3,
            Margin = new Thickness(4, 8)
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
                Text = subscription.AmountLabel,
                FontWeight = FontWeight.Medium
            }
            .Grid_Column(1),
            new TextBlock
            {
                Text = $"{subscription.ProviderLabel} · {subscription.NextBillingLabel}",
                FontSize = 11,
                Opacity = 0.62,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
            .Grid_Row(1),
            BuildStatus(subscription)
                .Grid_Column(1)
                .Grid_Row(1));
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
        var edit = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_Edit"));
        edit.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.EditSubscriptionCommand)));

        var archive = new Button();
        archive.Bind(
            ContentControl.ContentProperty,
            new Binding(nameof(SubscriptionsViewModel.ArchiveActionLabel)));
        archive.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.ToggleArchiveCommand)));

        var delete = new Button
        {
            Content = AppResources.Get("Subscriptions_Delete")
        };
        delete.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.RequestDeleteSubscriptionCommand)));

        return new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 0, 4, 12)
        }
        .Children(
            UiFactory.Card(
                new StackPanel
                {
                    Spacing = 18
                }
                .Children(
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        ColumnSpacing = 14
                    }
                    .Children(
                        new StackPanel
                        {
                            Spacing = 3
                        }
                        .Children(
                            UiFactory.BoundText(
                                SelectedPath(nameof(SubscriptionListItemViewModel.ServiceLabel)),
                                22,
                                FontWeight.SemiBold,
                                textWrapping: TextWrapping.Wrap),
                            UiFactory.BoundText(
                                SelectedPath(nameof(SubscriptionListItemViewModel.ProviderLabel)),
                                12,
                                opacity: 0.62)),
                        new StackPanel
                        {
                            Spacing = 6,
                            HorizontalAlignment = HorizontalAlignment.Right
                        }
                        .Children(
                            UiFactory.BoundText(
                                SelectedPath(nameof(SubscriptionListItemViewModel.AmountLabel)),
                                20,
                                FontWeight.SemiBold),
                            BuildBoundPill(
                                SelectedPath(nameof(SubscriptionListItemViewModel.BudgetStateLabel))))
                        .Grid_Column(1)),
                    BuildDetailGrid(),
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8
                    }
                    .Children(edit, archive, delete))),
            BuildDeleteConfirmation());
    }

    private static Control BuildDetailGrid()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("Auto,Auto,Auto,Auto"),
            ColumnSpacing = 18,
            RowSpacing = 14
        }
        .Children(
            BuildDetailValue(
                    AppResources.Get("Subscriptions_Schedule"),
                    SelectedPath(nameof(SubscriptionListItemViewModel.ScheduleLabel)),
                    SelectedPath(nameof(SubscriptionListItemViewModel.NextBillingLabel)))
                .Grid_Column(0)
                .Grid_Row(0),
            BuildDetailValue(
                    AppResources.Get("Column_Status"),
                    SelectedPath(nameof(SubscriptionListItemViewModel.LifecycleLabel)),
                    SelectedPath(nameof(SubscriptionListItemViewModel.StatusLabel)))
                .Grid_Column(1)
                .Grid_Row(0),
            BuildDetailValue(
                    AppResources.Get("Subscriptions_Importance"),
                    SelectedPath(nameof(SubscriptionListItemViewModel.ImportanceLabel)),
                    SelectedPath(nameof(SubscriptionListItemViewModel.CategoryLabel)))
                .Grid_Column(0)
                .Grid_Row(1),
            BuildDetailValue(
                    AppResources.Get("Editor_Account"),
                    SelectedPath(nameof(SubscriptionListItemViewModel.AccountLabel)),
                    SelectedPath(nameof(SubscriptionListItemViewModel.ArchiveStateLabel)))
                .Grid_Column(1)
                .Grid_Row(1),
            BuildDetailValue(
                    AppResources.Get("Subscriptions_Details"),
                    SelectedPath(nameof(SubscriptionListItemViewModel.SpecializedDetailsLabel)),
                    SelectedPath(nameof(SubscriptionListItemViewModel.ManagementUrlLabel)))
                .Grid_ColumnSpan(2)
                .Grid_Row(2),
            BuildDetailValue(
                    AppResources.Get("Editor_Notes"),
                    SelectedPath(nameof(SubscriptionListItemViewModel.NotesLabel)),
                    null)
                .Grid_ColumnSpan(2)
                .Grid_Row(3));
    }

    private static Control BuildDetailValue(string label, string primaryPath, string? secondaryPath)
    {
        var values = new StackPanel
        {
            Spacing = 3
        }
        .Children(
            new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Opacity = 0.56
            },
            UiFactory.BoundText(
                primaryPath,
                13,
                FontWeight.SemiBold,
                textWrapping: TextWrapping.Wrap));

        if (secondaryPath is not null)
        {
            values.Children.Add(UiFactory.BoundText(
                secondaryPath,
                11,
                opacity: 0.62,
                textWrapping: TextWrapping.Wrap));
        }

        return values;
    }

    private static Control BuildBoundPill(string propertyPath)
    {
        return new Border
        {
            Background = UiPalette.AccentSurface,
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(8, 4),
            HorizontalAlignment = HorizontalAlignment.Right,
            Child = UiFactory.BoundText(propertyPath, 10, FontWeight.Medium)
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
