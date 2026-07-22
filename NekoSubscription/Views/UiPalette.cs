using Avalonia.Media;

namespace NekoSubscription.Views;

internal static class UiPalette
{
    public static IBrush Accent { get; } = new SolidColorBrush(Color.FromRgb(124, 92, 255));

    public static IBrush AccentSurface { get; } = new SolidColorBrush(Color.FromArgb(42, 124, 92, 255));

    public static IBrush Border { get; } = new SolidColorBrush(Color.FromArgb(54, 127, 127, 127));

    public static IBrush DangerSurface { get; } = new SolidColorBrush(Color.FromArgb(40, 239, 68, 68));

    public static IBrush SidebarSurface { get; } = new SolidColorBrush(Color.FromArgb(25, 127, 127, 127));

    public static IBrush Success { get; } = new SolidColorBrush(Color.FromRgb(29, 185, 140));

    public static IBrush SuccessSurface { get; } = new SolidColorBrush(Color.FromArgb(42, 29, 185, 140));

    public static IBrush Surface { get; } = new SolidColorBrush(Color.FromArgb(28, 127, 127, 127));

    public static IBrush SurfaceStrong { get; } = new SolidColorBrush(Color.FromArgb(42, 127, 127, 127));

    public static IBrush Warning { get; } = new SolidColorBrush(Color.FromRgb(234, 157, 49));

    public static IBrush WarningSurface { get; } = new SolidColorBrush(Color.FromArgb(42, 234, 157, 49));
}
