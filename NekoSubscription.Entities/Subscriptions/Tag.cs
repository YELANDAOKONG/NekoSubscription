using System;
using System.Collections.Generic;

namespace NekoSubscription.Entities.Subscriptions;

public sealed class Tag
{
    public const int MaximumNameLength = 64;

    private Tag()
    {
    }

    public Tag(string name)
    {
        Id = Guid.NewGuid();
        Rename(name);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ICollection<Subscription> Subscriptions { get; } = new List<Subscription>();

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaximumNameLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                normalizedName.Length,
                $"The tag name cannot exceed {MaximumNameLength} characters.");
        }

        Name = normalizedName;
    }
}
