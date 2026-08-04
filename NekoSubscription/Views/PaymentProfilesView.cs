using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;

using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class PaymentProfilesView : UserControl
{
    public PaymentProfilesView()
    {
        var list = new ListBox { MinHeight = 260 };
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PaymentProfilesViewModel.Profiles)));
        list.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty, new Binding(nameof(PaymentProfilesViewModel.SelectedProfile)) { Mode = BindingMode.TwoWay });
        var refresh = new Button { Content = AppResources.Get("Common_Refresh") };
        refresh.Bind(Button.CommandProperty, new Binding(nameof(PaymentProfilesViewModel.RefreshCommand)));
        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(Button.CommandProperty, new Binding(nameof(PaymentProfilesViewModel.SaveCommand)));
        var archive = new Button();
        archive.Bind(Button.ContentProperty, new Binding(nameof(PaymentProfilesViewModel.ArchiveActionLabel)));
        archive.Bind(Button.CommandProperty, new Binding(nameof(PaymentProfilesViewModel.ToggleArchiveCommand)));
        archive.Bind(IsEnabledProperty, new Binding(nameof(PaymentProfilesViewModel.HasSelection)));

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = UiFactory.Card(
                new StackPanel { Spacing = 14 }.Children(
                    new StackPanel { Spacing = 4 }.Children(
                        UiFactory.SectionTitle(AppResources.Get("Nav_PaymentAndTags")),
                        new TextBlock { Text = AppResources.Get("Page_PaymentAndTagsSubtitle"), Opacity = 0.66 }),
                    new Grid { ColumnDefinitions = new ColumnDefinitions("240,*"), ColumnSpacing = 18 }.Children(
                        list,
                        new StackPanel { Spacing = 8 }.Children(
                            BuildTextBox(nameof(PaymentProfilesViewModel.DisplayName), "Editor_PaymentProfile"),
                            BuildTextBox(nameof(PaymentProfilesViewModel.ProviderName), "Editor_Provider"),
                            BuildTextBox(nameof(PaymentProfilesViewModel.AccountIdentifier), "Editor_PaymentAccount"),
                            BuildComboBox(),
                            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }.Children(save, archive, refresh)).Grid_Column(1))),
                new Thickness(16))
        };
    }

    private static Control BuildTextBox(string path, string placeholderKey)
    {
        var textBox = new TextBox { PlaceholderText = AppResources.Get(placeholderKey) };
        textBox.Bind(TextBox.TextProperty, new Binding(path) { Mode = BindingMode.TwoWay });
        return textBox;
    }

    private static Control BuildComboBox()
    {
        var comboBox = new ComboBox();
        comboBox.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PaymentProfilesViewModel.Channels)));
        comboBox.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty, new Binding(nameof(PaymentProfilesViewModel.SelectedChannel)) { Mode = BindingMode.TwoWay });
        return comboBox;
    }
}
