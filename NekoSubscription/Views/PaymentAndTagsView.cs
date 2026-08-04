using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class PaymentAndTagsView : UserControl
{
    public PaymentAndTagsView()
    {
        var refresh = new Button { Content = AppResources.Get("Common_Refresh") };
        refresh.Bind(Button.CommandProperty, new Binding(nameof(PaymentAndTagsViewModel.RefreshCommand)));

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 16,
                Margin = new Thickness(0, 0, 8, 8),
                MaxWidth = 920
            }
            .Children(
                BuildPaymentCard(),
                BuildTagsCard())
        };
    }

    private static Control BuildPaymentCard()
    {
        var list = new ListBox { MinHeight = 180, SelectionMode = SelectionMode.Single };
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PaymentAndTagsViewModel.PaymentProfiles)));
        list.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(PaymentAndTagsViewModel.SelectedPaymentProfile)) { Mode = BindingMode.TwoWay });

        var name = BuildTextBox(nameof(PaymentAndTagsViewModel.PaymentDisplayName), AppResources.Get("Editor_PaymentProfile"));
        var provider = BuildTextBox(nameof(PaymentAndTagsViewModel.PaymentProviderName), AppResources.Get("Editor_Provider"));
        var account = BuildTextBox(nameof(PaymentAndTagsViewModel.PaymentAccountIdentifier), AppResources.Get("Editor_PaymentAccount"));
        var channel = new ComboBox();
        channel.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PaymentAndTagsViewModel.PaymentChannels)));
        channel.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(PaymentAndTagsViewModel.SelectedPaymentChannel)) { Mode = BindingMode.TwoWay });

        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(Button.CommandProperty, new Binding(nameof(PaymentAndTagsViewModel.SavePaymentProfileCommand)));
        var archive = new Button();
        archive.Bind(Button.ContentProperty, new Binding(nameof(PaymentAndTagsViewModel.PaymentArchiveActionLabel)));
        archive.Bind(Button.CommandProperty, new Binding(nameof(PaymentAndTagsViewModel.TogglePaymentProfileArchiveCommand)));
        archive.Bind(IsEnabledProperty, new Binding(nameof(PaymentAndTagsViewModel.HasSelectedPaymentProfile)));

        return UiFactory.Card(
            new StackPanel { Spacing = 12 }.Children(
                BuildHeading("Settings_PaymentProfiles", "Settings_PaymentAndTagsDescription"),
                new Grid { ColumnDefinitions = new ColumnDefinitions("220,*"), ColumnSpacing = 18 }.Children(
                    list,
                    new StackPanel { Spacing = 8 }.Children(
                        name,
                        provider,
                        account,
                        channel,
                        new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }.Children(save, archive)).Grid_Column(1))),
            new Thickness(16));
    }

    private static Control BuildTagsCard()
    {
        var list = new ListBox { MinHeight = 180, SelectionMode = SelectionMode.Single };
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PaymentAndTagsViewModel.Tags)));
        list.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(PaymentAndTagsViewModel.SelectedTag)) { Mode = BindingMode.TwoWay });

        var name = BuildTextBox(nameof(PaymentAndTagsViewModel.TagName), AppResources.Get("Editor_Tags"));
        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(Button.CommandProperty, new Binding(nameof(PaymentAndTagsViewModel.SaveTagCommand)));

        return UiFactory.Card(
            new StackPanel { Spacing = 12 }.Children(
                BuildHeading("Settings_Tags", "Settings_TagsDescription"),
                new Grid { ColumnDefinitions = new ColumnDefinitions("220,*"), ColumnSpacing = 18 }.Children(
                    list,
                    new StackPanel { Spacing = 8 }.Children(
                        name,
                        save).Grid_Column(1))),
            new Thickness(16));
    }

    private static Control BuildTextBox(string propertyPath, string placeholder)
    {
        var textBox = new TextBox { PlaceholderText = placeholder };
        textBox.Bind(TextBox.TextProperty, new Binding(propertyPath) { Mode = BindingMode.TwoWay });
        return textBox;
    }

    private static Control BuildHeading(string titleKey, string descriptionKey) =>
        new StackPanel { Spacing = 4 }.Children(
            new TextBlock { Text = AppResources.Get(titleKey), FontSize = 16, FontWeight = FontWeight.SemiBold },
            new TextBlock { Text = AppResources.Get(descriptionKey), Opacity = 0.66, TextWrapping = TextWrapping.Wrap });
}
