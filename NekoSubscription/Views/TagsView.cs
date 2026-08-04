using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Styling;

using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class TagsView : UserControl
{
    public TagsView()
    {
        Content = new Grid { RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 16, Margin = new Thickness(0, 0, 8, 14) }
            .Children(BuildToolbar().Grid_Row(0), BuildWorkspace().Grid_Row(1));
    }

    private static Control BuildToolbar()
    {
        var add = UiFactory.PrimaryButton(AppResources.Get("Settings_AddTag"), AppIcons.Add);
        add.Bind(Button.CommandProperty, new Binding(nameof(TagsViewModel.AddTagCommand)));
        var refresh = new Button
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 }.Children(
                UiFactory.Icon(AppIcons.Refresh, 14),
                new TextBlock { Text = AppResources.Get("Common_Refresh"), VerticalAlignment = VerticalAlignment.Center })
        };
        refresh.Bind(Button.CommandProperty, new Binding(nameof(TagsViewModel.RefreshCommand)));

        return UiFactory.Card(new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 10 }.Children(
            new StackPanel { Spacing = 4 }.Children(
                UiFactory.SectionTitle(AppResources.Get("Nav_Tags")),
                new TextBlock { Text = AppResources.Get("Page_TagsSubtitle"), Opacity = 0.66, TextWrapping = TextWrapping.Wrap }),
            refresh.Grid_Column(1), add.Grid_Column(2)), new Thickness(16));
    }

    private static Control BuildWorkspace()
    {
        return new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 }.Children(
            BuildList().Grid_Column(0), BuildEditorPane().Grid_Column(1));
    }

    private static Control BuildList()
    {
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<Tag>((tag, _) => BuildRow(tag)),
            Background = Brushes.Transparent,
            SelectionMode = SelectionMode.Single | SelectionMode.Toggle
        };
        list.Styles.Add(new Style(x => x.OfType<ListBoxItem>())
        {
            Setters = { new Setter(CornerRadiusProperty, new CornerRadius(12)), new Setter(MarginProperty, new Thickness(0, 0, 0, 4)) }
        });
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(TagsViewModel.Tags)));
        list.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(TagsViewModel.SelectedTag)) { Mode = BindingMode.TwoWay });
        return UiFactory.Card(new Grid().Children(list), new Thickness(4));
    }

    private static Control BuildRow(Tag? tag)
    {
        if (tag is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_Unknown") };
        }

        return new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 14, Margin = new Thickness(4, 8) }.Children(
            BuildAvatar(tag.Name).Grid_Column(0),
            new TextBlock
            {
                Text = tag.Name,
                FontWeight = FontWeight.SemiBold,
                FontSize = 14,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            }.Grid_Column(1));
    }

    private static Control BuildEditorPane()
    {
        var editor = new ScrollViewer { MaxWidth = 460, MinWidth = 340,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = UiFactory.Card(BuildEditor(), new Thickness(24)) };
        editor.Bind(IsVisibleProperty, new Binding(nameof(TagsViewModel.HasEditor)));
        return editor;
    }

    private static Control BuildEditor()
    {
        var name = new TextBox
        {
            PlaceholderText = AppResources.Get("Editor_Tags"),
            CornerRadius = new CornerRadius(8)
        };
        name.Bind(TextBox.TextProperty, new Binding(nameof(TagsViewModel.Name)) { Mode = BindingMode.TwoWay });

        var cancel = new Button
        {
            Content = AppResources.Get("Common_Cancel"),
            MinWidth = 92,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        cancel.Bind(Button.CommandProperty, new Binding(nameof(TagsViewModel.CancelEditCommand)));

        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(Button.CommandProperty, new Binding(nameof(TagsViewModel.SaveCommand)));

        var heroHeader = new StackPanel
        {
            Spacing = 16,
            HorizontalAlignment = HorizontalAlignment.Center
        }.Children(
            BuildBoundAvatar(nameof(TagsViewModel.Name), 64, 28),
            new StackPanel
            {
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center
            }.Children(
                UiFactory.BoundText(
                    nameof(TagsViewModel.EditorTitle),
                    24,
                    FontWeight.Bold,
                    textAlignment: TextAlignment.Center),
                new TextBlock
                {
                    Text = AppResources.Get("Page_TagsSubtitle"),
                    FontSize = 13,
                    Opacity = 0.62,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                }));

        var actionBar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        }.Children(cancel, save);

        return new StackPanel { Spacing = 20 }.Children(heroHeader, name, actionBar);
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
                Converter = new FuncValueConverter<string?, string>(value =>
                    string.IsNullOrWhiteSpace(value) ? "?" : value.Substring(0, 1).ToUpperInvariant())
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

    private static Control BuildAvatar(string name)
    {
        var letter = string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
        return new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(20), Background = UiPalette.AccentSurface,
            Child = new TextBlock { Text = letter, Foreground = UiPalette.Accent, FontSize = 18, FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
    }
}
