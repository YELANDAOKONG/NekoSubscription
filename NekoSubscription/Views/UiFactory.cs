using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

namespace NekoSubscription.Views;

internal static class UiFactory
{
    private const double CardCornerRadius = 14;

    public static Border Card(Control child, Thickness? padding = null)
    {
        return new Border
        {
            Background = UiPalette.Surface,
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(CardCornerRadius),
            Padding = padding ?? new Thickness(18),
            Child = child
        };
    }

    public static Border EmptyState(string title, string description)
    {
        return new Border
        {
            Background = UiPalette.Surface,
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(CardCornerRadius),
            Padding = new Thickness(32),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = new StackPanel
            {
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 420
            }
            .Children(
                new Border
                {
                    Width = 44,
                    Height = 44,
                    Background = UiPalette.AccentSurface,
                    CornerRadius = new CornerRadius(22),
                    Child = new TextBlock
                    {
                        Text = "—",
                        FontSize = 20,
                        FontWeight = FontWeight.SemiBold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                },
                new TextBlock
                {
                    Text = title,
                    FontSize = 17,
                    FontWeight = FontWeight.SemiBold,
                    TextAlignment = TextAlignment.Center
                },
                new TextBlock
                {
                    Text = description,
                    Opacity = 0.68,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap
                })
        };
    }

    public static Button PrimaryButton(string text)
    {
        var button = new Button
        {
            Content = text,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            MinWidth = 112
        };
        button.Classes.Add("accent");
        return button;
    }

    public static TextBlock BoundText(
        string path,
        double fontSize = 14,
        FontWeight? fontWeight = null,
        double opacity = 1,
        TextWrapping textWrapping = TextWrapping.NoWrap)
    {
        var textBlock = new TextBlock
        {
            FontSize = fontSize,
            FontWeight = fontWeight ?? FontWeight.Normal,
            Opacity = opacity,
            TextWrapping = textWrapping
        };
        textBlock.Bind(TextBlock.TextProperty, new Binding(path));
        return textBlock;
    }

    public static TextBlock SectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 17,
            FontWeight = FontWeight.SemiBold
        };
    }

    public static Border StatusPill(string text, IBrush background)
    {
        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(9, 4),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeight.Medium
            }
        };
    }
}
