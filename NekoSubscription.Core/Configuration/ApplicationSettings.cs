using System;

namespace NekoSubscription.Core.Configuration;

public sealed class ApplicationSettings
{
    public const double DefaultAcrylicOpacity = 0.85;
    public const double MaximumAcrylicOpacity = 1.0;
    public const double MinimumAcrylicOpacity = 0.2;
    public const int SingletonId = 1;
    public const int MaximumCultureNameLength = 32;

    public int Id { get; private set; } = SingletonId;

    public ApplicationTheme Theme { get; set; } = ApplicationTheme.System;

    public ApplicationVisualStyle VisualStyle { get; set; } = ApplicationVisualStyle.Standard;

    public double AcrylicOpacity { get; set; } = DefaultAcrylicOpacity;

    public string? CultureName { get; set; }

    public ApplicationLogLevel MinimumLogLevel { get; set; } = ApplicationLogLevel.Information;

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    internal void MarkUpdated(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
    }
}
