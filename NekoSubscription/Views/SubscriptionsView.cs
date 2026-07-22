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
        var workspace = BuildWorkspace();
        workspace.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasNoEditor)));

        var editor = new ContentControl();
        editor.Bind(
            ContentControl.ContentProperty,
            new Binding(nameof(SubscriptionsViewModel.CurrentEditor)));
        editor.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasEditor)));

        Content = new Grid()
            .Children(workspace, editor);
    }

    private static Grid BuildWorkspace()
    {
        return new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 14,
            Margin = new Thickness(0, 0, 8, 8)
        }
        .Children(
            BuildToolbar().Grid_Row(0),
            BuildSubscriptionList().Grid_Row(1),
            BuildDetails().Grid_Row(2));
    }

    private static Control BuildToolbar()
    {
        var searchBox = new TextBox
        {
            PlaceholderText = AppResources.Get("Subscriptions_SearchPlaceholder"),
            MinWidth = 260,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        searchBox.Bind(
            TextBox.TextProperty,
            new Binding(nameof(SubscriptionsViewModel.SearchText))
            {
                Mode = BindingMode.TwoWay
            });

        var categoryFilter = new ComboBox
        {
            MinWidth = 160
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

        var refreshButton = new Button
        {
            Content = AppResources.Get("Subscriptions_Refresh"),
            MinWidth = 90
        };
        refreshButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.RefreshCommand)));

        var addButton = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_Add"));
        addButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.AddSubscriptionCommand)));

        return UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto,Auto"),
                ColumnSpacing = 10
            }
            .Children(
                searchBox.Grid_Column(0),
                categoryFilter.Grid_Column(1),
                includeArchived.Grid_Column(2),
                refreshButton.Grid_Column(3),
                addButton.Grid_Column(4)),
            new Thickness(12));
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

        var emptyState = UiFactory.EmptyState(
            AppResources.Get("Subscriptions_EmptyTitle"),
            AppResources.Get("Subscriptions_EmptyDescription"));
        emptyState.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasNoResults)));

        var body = new Grid()
            .Children(list, emptyState);

        return UiFactory.Card(
            new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,Auto,*"),
                RowSpacing = 10
            }
            .Children(
                BuildListTitle().Grid_Row(0),
                BuildColumnHeaders().Grid_Row(1),
                body.Grid_Row(2)),
            new Thickness(14));
    }

    private static Control BuildListTitle()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        }
        .Children(
            UiFactory.SectionTitle(AppResources.Get("Subscriptions_All")),
            UiFactory.BoundText(
                    nameof(SubscriptionsViewModel.ResultSummary),
                    12,
                    opacity: 0.62)
                .Grid_Column(1));
    }

    private static Control BuildColumnHeaders()
    {
        return new Border
        {
            Background = UiPalette.SurfaceStrong,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 7),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("2.2*,1.15*,1.05*,1*,1*"),
                ColumnSpacing = 12
            }
            .Children(
                BuildColumnHeader(AppResources.Get("Column_Service")).Grid_Column(0),
                BuildColumnHeader(AppResources.Get("Column_Category")).Grid_Column(1),
                BuildColumnHeader(AppResources.Get("Column_Status")).Grid_Column(2),
                BuildColumnHeader(AppResources.Get("Column_Amount")).Grid_Column(3),
                BuildColumnHeader(AppResources.Get("Column_NextBilling")).Grid_Column(4))
        };
    }

    private static Control BuildSubscriptionRow(SubscriptionListItemViewModel? subscription)
    {
        if (subscription is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_SubscriptionUnavailable") };
        }

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2.2*,1.15*,1.05*,1*,1*"),
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
                    Text = $"{subscription.ProviderLabel} · {subscription.AccountLabel}",
                    FontSize = 12,
                    Opacity = 0.62,
                    TextTrimming = TextTrimming.CharacterEllipsis
                })
            .Grid_Column(0),
            new TextBlock
            {
                Text = subscription.CategoryLabel,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
            .Grid_Column(1),
            BuildStatus(subscription)
                .Grid_Column(2),
            new TextBlock
            {
                Text = subscription.AmountLabel,
                FontWeight = FontWeight.Medium,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Grid_Column(3),
            new TextBlock
            {
                Text = subscription.NextBillingLabel,
                VerticalAlignment = VerticalAlignment.Center
            }
            .Grid_Column(4));
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
            Padding = new Thickness(8, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = subscription.IsArchived
                    ? AppResources.Get("Details_Archived")
                    : subscription.LifecycleLabel,
                FontSize = 11,
                FontWeight = FontWeight.Medium,
                TextTrimming = TextTrimming.CharacterEllipsis
            }
        };
    }

    private static Control BuildDetails()
    {
        var editButton = new Button
        {
            Content = AppResources.Get("Subscriptions_Edit")
        };
        editButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.EditSubscriptionCommand)));
        editButton.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionsViewModel.HasSelectedSubscription)));

        var deleteButton = new Button
        {
            Content = AppResources.Get("Subscriptions_Delete")
        };
        deleteButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.RequestDeleteSubscriptionCommand)));
        deleteButton.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionsViewModel.HasSelectedSubscription)));

        var archiveButton = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_Archive"));
        archiveButton.Bind(
            ContentControl.ContentProperty,
            new Binding(nameof(SubscriptionsViewModel.ArchiveActionLabel)));
        archiveButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.ToggleArchiveCommand)));
        archiveButton.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionsViewModel.HasSelectedSubscription)));

        var details = UiFactory.Card(
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("2*,1.2*,1.2*,1.2*,Auto"),
                ColumnSpacing = 18
            }
            .Children(
                BuildDetailValue(
                        AppResources.Get("Subscriptions_Selected"),
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.ServiceLabel)}",
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.ProviderLabel)}")
                    .Grid_Column(0),
                BuildDetailValue(
                        AppResources.Get("Subscriptions_Schedule"),
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.ScheduleLabel)}",
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.NextBillingLabel)}")
                    .Grid_Column(1),
                BuildDetailValue(
                        AppResources.Get("Subscriptions_Importance"),
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.ImportanceLabel)}",
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.StatusLabel)}")
                    .Grid_Column(2),
                BuildDetailValue(
                        AppResources.Get("Subscriptions_Details"),
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.SpecializedDetailsLabel)}",
                        $"{nameof(SubscriptionsViewModel.SelectedSubscription)}.{nameof(SubscriptionListItemViewModel.ManagementUrlLabel)}")
                    .Grid_Column(3),
                new StackPanel
                {
                    Spacing = 7,
                    VerticalAlignment = VerticalAlignment.Center
                }
                .Children(editButton, archiveButton, deleteButton)
                    .Grid_Column(4)));
        details.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.HasSelectedSubscription)));

        var cancelDeleteButton = new Button
        {
            Content = AppResources.Get("Common_Cancel")
        };
        cancelDeleteButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.CancelDeleteSubscriptionCommand)));

        var confirmDeleteButton = UiFactory.PrimaryButton(AppResources.Get("Subscriptions_ConfirmDelete"));
        confirmDeleteButton.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionsViewModel.ConfirmDeleteSubscriptionCommand)));

        var confirmation = new Border
        {
            Background = UiPalette.DangerSurface,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                ColumnSpacing = 10
            }
            .Children(
                new TextBlock
                {
                    Text = AppResources.Get("Subscriptions_DeleteConfirmation"),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                },
                cancelDeleteButton.Grid_Column(1),
                confirmDeleteButton.Grid_Column(2))
        };
        confirmation.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionsViewModel.IsDeleteConfirmationVisible)));

        return new StackPanel
        {
            Spacing = 8
        }
        .Children(details, confirmation);
    }

    private static Control BuildDetailValue(string label, string primaryPath, string secondaryPath)
    {
        return new StackPanel
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
            UiFactory.BoundText(primaryPath, 13, FontWeight.SemiBold, textWrapping: TextWrapping.Wrap),
            UiFactory.BoundText(secondaryPath, 11, opacity: 0.62, textWrapping: TextWrapping.Wrap));
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
