using System;

namespace NekoSubscription.Core.Configuration;

public sealed class SettingEntry
{
    public const int MaximumKeyLength = 256;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
