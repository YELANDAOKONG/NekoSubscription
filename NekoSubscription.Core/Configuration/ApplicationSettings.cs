using System;

namespace NekoSubscription.Core.Configuration;

public sealed class ApplicationSettings
{
    public const int SingletonId = 1;
    public const int MaximumCultureNameLength = 32;

    public int Id { get; private set; } = SingletonId;

    public ApplicationTheme Theme { get; set; } = ApplicationTheme.System;

    public string? CultureName { get; set; }

    public ApplicationLogLevel MinimumLogLevel { get; set; } = ApplicationLogLevel.Information;

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    internal void MarkUpdated(DateTimeOffset updatedAtUtc)
    {
        UpdatedAtUtc = updatedAtUtc;
    }
}
