using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;

using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class TagsView : UserControl
{
    public TagsView()
    {
        var list = new ListBox { MinHeight = 260 };
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(TagsViewModel.Tags)));
        list.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty, new Binding(nameof(TagsViewModel.SelectedTag)) { Mode = BindingMode.TwoWay });
        var name = new TextBox { PlaceholderText = AppResources.Get("Editor_Tags") };
        name.Bind(TextBox.TextProperty, new Binding(nameof(TagsViewModel.Name)) { Mode = BindingMode.TwoWay });
        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(Button.CommandProperty, new Binding(nameof(TagsViewModel.SaveCommand)));
        var refresh = new Button { Content = AppResources.Get("Common_Refresh") };
        refresh.Bind(Button.CommandProperty, new Binding(nameof(TagsViewModel.RefreshCommand)));

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = UiFactory.Card(
                new StackPanel { Spacing = 14 }.Children(
                    new StackPanel { Spacing = 4 }.Children(
                        UiFactory.SectionTitle(AppResources.Get("Nav_Tags")),
                        new TextBlock { Text = AppResources.Get("Settings_TagsDescription"), Opacity = 0.66 }),
                    new Grid { ColumnDefinitions = new ColumnDefinitions("240,*"), ColumnSpacing = 18 }.Children(
                        list,
                        new StackPanel { Spacing = 8 }.Children(
                            name,
                            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 }.Children(save, refresh)).Grid_Column(1))),
                new Thickness(16))
        };
    }
}
