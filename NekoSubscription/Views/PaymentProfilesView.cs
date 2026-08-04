using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Styling;

using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class PaymentProfilesView : UserControl
{
    public PaymentProfilesView()
    {
        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            RowSpacing = 16,
            Margin = new Thickness(0, 0, 8, 14)
        }
        .Children(BuildToolbar().Grid_Row(0), BuildWorkspace().Grid_Row(1));
    }

    private static Control BuildToolbar()
    {
        var add = UiFactory.PrimaryButton(AppResources.Get("Settings_AddPaymentProfile"), AppIcons.Add);
        add.Bind(Button.CommandProperty, new Binding(nameof(PaymentProfilesViewModel.AddPaymentProfileCommand)));

        var refresh = new Button
        {
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 }.Children(
                UiFactory.Icon(AppIcons.Refresh, 14),
                new TextBlock { Text = AppResources.Get("Common_Refresh"), VerticalAlignment = VerticalAlignment.Center })
        };
        refresh.Bind(Button.CommandProperty, new Binding(nameof(PaymentProfilesViewModel.RefreshCommand)));

        return UiFactory.Card(
            new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 10 }.Children(
                new StackPanel { Spacing = 4 }.Children(
                    UiFactory.SectionTitle(AppResources.Get("Nav_PaymentAndTags")),
                    new TextBlock { Text = AppResources.Get("Page_PaymentAndTagsSubtitle"), Opacity = 0.66, TextWrapping = TextWrapping.Wrap }),
                refresh.Grid_Column(1),
                add.Grid_Column(2)),
            new Thickness(16));
    }

    private static Control BuildWorkspace()
    {
        return new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 }.Children(
            BuildList().Grid_Column(0),
            BuildEditorPane().Grid_Column(1));
    }

    private static Control BuildList()
    {
        var list = new ListBox
        {
            ItemTemplate = new FuncDataTemplate<PaymentProfile>((profile, _) => BuildRow(profile)),
            Background = Brushes.Transparent,
            SelectionMode = SelectionMode.Single | SelectionMode.Toggle
        };
        list.Styles.Add(new Style(x => x.OfType<ListBoxItem>())
        {
            Setters = { new Setter(CornerRadiusProperty, new CornerRadius(12)), new Setter(MarginProperty, new Thickness(0, 0, 0, 4)) }
        });
        list.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PaymentProfilesViewModel.Profiles)));
        list.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(PaymentProfilesViewModel.SelectedProfile)) { Mode = BindingMode.TwoWay });

        return UiFactory.Card(new Grid().Children(list), new Thickness(4));
    }

    private static Control BuildRow(PaymentProfile? profile)
    {
        if (profile is null)
        {
            return new TextBlock { Text = AppResources.Get("Common_Unknown") };
        }

        var description = string.IsNullOrWhiteSpace(profile.ProviderName)
            ? profile.AccountIdentifier ?? string.Empty
            : profile.ProviderName;
        var status = profile.IsArchived
            ? UiFactory.StatusPill(AppResources.Get("Settings_ArchivePaymentProfile"), UiPalette.SurfaceStrong)
            : UiFactory.StatusPill(AppResources.Get("Payment_" + profile.Channel), UiPalette.SuccessSurface);

        return new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 14, Margin = new Thickness(4, 8) }.Children(
            BuildAvatar(profile.DisplayName).Grid_Column(0),
            new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center }.Children(
                new TextBlock { Text = profile.DisplayName, FontWeight = FontWeight.SemiBold, FontSize = 14, TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Text = description, FontSize = 11, Opacity = 0.62, TextTrimming = TextTrimming.CharacterEllipsis }).Grid_Column(1),
            status.Grid_Column(2));
    }

    private static Control BuildEditorPane()
    {
        var editor = new ScrollViewer
        {
            MaxWidth = 460,
            MinWidth = 340,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = UiFactory.Card(BuildEditor(), new Thickness(24))
        };
        editor.Bind(IsVisibleProperty, new Binding(nameof(PaymentProfilesViewModel.HasEditor)));
        return editor;
    }

    private static Control BuildEditor()
    {
        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(Button.CommandProperty, new Binding(nameof(PaymentProfilesViewModel.SaveCommand)));
        var archive = new Button();
        archive.Bind(Button.ContentProperty, new Binding(nameof(PaymentProfilesViewModel.ArchiveActionLabel)));
        archive.Bind(Button.CommandProperty, new Binding(nameof(PaymentProfilesViewModel.ToggleArchiveCommand)));

        return new StackPanel { Spacing = 16 }.Children(
            new StackPanel { Spacing = 4 }.Children(
                UiFactory.BoundText(nameof(PaymentProfilesViewModel.DisplayName), 24, FontWeight.Bold),
                UiFactory.BoundText(nameof(PaymentProfilesViewModel.ProviderName), 13, opacity: 0.62)),
            BuildField(AppResources.Get("Editor_PaymentProfile"), BuildTextBox(nameof(PaymentProfilesViewModel.DisplayName), "Editor_PaymentProfile")),
            BuildField(AppResources.Get("Editor_Provider"), BuildTextBox(nameof(PaymentProfilesViewModel.ProviderName), "Editor_Provider")),
            BuildField(AppResources.Get("Editor_PaymentAccount"), BuildTextBox(nameof(PaymentProfilesViewModel.AccountIdentifier), "Editor_PaymentAccount")),
            BuildField(AppResources.Get("Editor_PaymentProfile"), BuildComboBox()),
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right }.Children(save, archive));
    }

    private static Control BuildField(string label, Control editor) => new StackPanel { Spacing = 5 }.Children(
        new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeight.Medium, Opacity = 0.72 }, editor);

    private static Control BuildTextBox(string path, string placeholderKey)
    {
        var textBox = new TextBox { PlaceholderText = AppResources.Get(placeholderKey), CornerRadius = new CornerRadius(8) };
        textBox.Bind(TextBox.TextProperty, new Binding(path) { Mode = BindingMode.TwoWay });
        return textBox;
    }

    private static Control BuildComboBox()
    {
        var comboBox = new ComboBox { CornerRadius = new CornerRadius(8) };
        comboBox.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(PaymentProfilesViewModel.Channels)));
        comboBox.Bind(Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(nameof(PaymentProfilesViewModel.SelectedChannel)) { Mode = BindingMode.TwoWay });
        return comboBox;
    }

    private static Control BuildAvatar(string name)
    {
        var letter = string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
        return new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(20), Background = UiPalette.AccentSurface,
            Child = new TextBlock { Text = letter, Foreground = UiPalette.Accent, FontSize = 18, FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
    }
}
