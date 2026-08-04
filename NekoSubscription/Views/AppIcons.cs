using Avalonia.Media;

namespace NekoSubscription.Views;

internal static class AppIcons
{
    public static Geometry Overview { get; } = StreamGeometry.Parse(
        "M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z");

    public static Geometry Calendar { get; } = StreamGeometry.Parse(
        "M19 4h-1V2h-2v2H8V2H6v2H5c-1.11 0-1.99.9-1.99 2L3 20c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 16H5V10h14v10zm-2-7h-5v5h5v-5z");

    public static Geometry Subscriptions { get; } = StreamGeometry.Parse(
        "M4 6h16v2H4zm2 4h12v2H6zm3 4h6v2H9zM19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14z");

    public static Geometry Settings { get; } = StreamGeometry.Parse(
        "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z");

    public static Geometry Payment { get; } = StreamGeometry.Parse(
        "M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-8H4V6h16v4z");

    public static Geometry Tags { get; } = StreamGeometry.Parse(
        "M20.59 13.41l-7.17 7.17c-.78.78-2.05.78-2.83 0L2 12V2h10l8.59 8.59c.78.78.78 2.04 0 2.82zM7.5 7C6.67 7 6 6.33 6 5.5S6.67 4 7.5 4 9 4.67 9 5.5 8.33 7 7.5 7z");

    public static Geometry PanelLeft { get; } = StreamGeometry.Parse(
        "M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2zm0 4v10h5V8H4zm7 10h9V8h-9v10z");

    public static Geometry Add { get; } = StreamGeometry.Parse(
        "M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z");

    public static Geometry Edit { get; } = StreamGeometry.Parse(
        "M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04c.39-.39.39-1.02 0-1.41l-2.34-2.34c-.39-.39-1.02-.39-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z");

    public static Geometry Delete { get; } = StreamGeometry.Parse(
        "M6 19c0 1.1.9 2 2 2h8c1.1 0 2-.9 2-2V7H6v12zM19 4h-3.5l-1-1h-5l-1 1H5v2h14V4z");

    public static Geometry Archive { get; } = StreamGeometry.Parse(
        "M20.54 5.23l-1.39-1.68C18.88 3.21 18.47 3 18 3H6c-.47 0-.88.21-1.16.55L3.46 5.23C3.17 5.57 3 6.02 3 6.5V19c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V6.5c0-.48-.17-.93-.46-1.27zM6.24 5h11.52l.83 1H5.41l.83-1zM5 19V8h14v11H5zm11-5.5l-4 4-4-4 1.41-1.41L11 13.67V10h2v3.67l1.59-1.58L16 13.5z");

    public static Geometry Refresh { get; } = StreamGeometry.Parse(
        "M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z");

    public static Geometry Search { get; } = StreamGeometry.Parse(
        "M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z");

    public static Geometry ChevronLeft { get; } = StreamGeometry.Parse(
        "M15.41 7.41L14 6l-6 6 6 6 1.41-1.41L10.83 12z");

    public static Geometry ChevronRight { get; } = StreamGeometry.Parse(
        "M10 6L8.59 7.41 13.17 12l-4.58 4.59L10 18l6-6z");

    public static Geometry Checkmark { get; } = StreamGeometry.Parse(
        "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z");

    public static Geometry Warning { get; } = StreamGeometry.Parse(
        "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z");

    public static Geometry ShieldCheck { get; } = StreamGeometry.Parse(
        "M12 1L3 5v6c0 5.55 3.84 10.74 9 12 5.16-1.26 9-6.45 9-12V5l-9-4zm-2 16l-4-4 1.41-1.41L10 14.17l6.59-6.59L18 9l-8 8z");

    public static Geometry Metrics { get; } = StreamGeometry.Parse(
        "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z");

    public static Geometry Backup { get; } = StreamGeometry.Parse(
        "M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96zM14 13v4h-4v-4H7l5-5 5 5h-3z");

    public static Geometry Import { get; } = StreamGeometry.Parse(
        "M19.35 10.04C18.67 6.59 15.64 4 12 4 9.11 4 6.6 5.64 5.35 8.04 2.34 8.36 0 10.91 0 14c0 3.31 2.69 6 6 6h13c2.76 0 5-2.24 5-5 0-2.64-2.05-4.78-4.65-4.96zM12 17l-5-5h3V8h4v4h3l-5 5z");

    public static Geometry Export { get; } = StreamGeometry.Parse(
        "M5 20h14v-2H5v2zm7-18L5.33 8.67l1.41 1.41L11 5.83V16h2V5.83l4.26 4.25 1.41-1.41L12 2z");
}
