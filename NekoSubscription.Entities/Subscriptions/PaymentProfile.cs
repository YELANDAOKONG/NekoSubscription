using System;
using System.Collections.Generic;

namespace NekoSubscription.Entities.Subscriptions;

public sealed class PaymentProfile
{
    public const int MaximumAccountIdentifierLength = 320;
    public const int MaximumDisplayNameLength = 200;
    public const int MaximumNotesLength = 1000;
    public const int MaximumProviderNameLength = 200;

    private PaymentProfile()
    {
    }

    public PaymentProfile(
        string displayName,
        PaymentChannel channel,
        string? accountIdentifier,
        string? providerName,
        string? notes)
    {
        Id = Guid.NewGuid();
        Update(displayName, channel, accountIdentifier, providerName, notes, DateTimeOffset.UtcNow);
        CreatedAtUtc = UpdatedAtUtc;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public PaymentChannel Channel { get; private set; }

    public string? AccountIdentifier { get; private set; }

    public string? ProviderName { get; private set; }

    public string? Notes { get; private set; }

    public bool IsArchived { get; private set; }

    public DateTimeOffset? ArchivedAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public ICollection<Subscription> Subscriptions { get; } = new List<Subscription>();

    public void Update(
        string displayName,
        PaymentChannel channel,
        string? accountIdentifier,
        string? providerName,
        string? notes,
        DateTimeOffset updatedAtUtc)
    {
        if (!Enum.IsDefined(channel))
        {
            throw new ArgumentOutOfRangeException(nameof(channel), channel, "The payment channel is invalid.");
        }

        var normalizedAccountIdentifier = NormalizeOptional(
            accountIdentifier,
            MaximumAccountIdentifierLength,
            nameof(accountIdentifier));

        if (RequiresAccountIdentifier(channel) && normalizedAccountIdentifier is null)
        {
            throw new ArgumentException(
                "This payment channel requires an account identifier.",
                nameof(accountIdentifier));
        }

        DisplayName = NormalizeRequired(displayName, MaximumDisplayNameLength, nameof(displayName));
        Channel = channel;
        AccountIdentifier = normalizedAccountIdentifier;
        ProviderName = NormalizeOptional(providerName, MaximumProviderNameLength, nameof(providerName));
        Notes = NormalizeOptional(notes, MaximumNotesLength, nameof(notes));
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Archive(DateTimeOffset archivedAtUtc)
    {
        IsArchived = true;
        ArchivedAtUtc = archivedAtUtc;
        UpdatedAtUtc = archivedAtUtc;
    }

    public void RestoreFromArchive(DateTimeOffset restoredAtUtc)
    {
        IsArchived = false;
        ArchivedAtUtc = null;
        UpdatedAtUtc = restoredAtUtc;
    }

    private static string NormalizeRequired(string value, int maximumLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                normalizedValue.Length,
                $"The value cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    private static string? NormalizeOptional(string? value, int maximumLength, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeRequired(value, maximumLength, parameterName);
    }

    private static bool RequiresAccountIdentifier(PaymentChannel channel)
    {
        return channel switch
        {
            PaymentChannel.AppleAppStore => true,
            PaymentChannel.GooglePlay => true,
            PaymentChannel.PayPal => true,
            _ => false
        };
    }
}
