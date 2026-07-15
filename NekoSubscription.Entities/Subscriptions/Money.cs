using System;
using System.Linq;

namespace NekoSubscription.Entities.Subscriptions;

public sealed record Money
{
    public const int MaximumCurrencyCodeLength = 10;

    private Money()
    {
        CurrencyCode = string.Empty;
    }

    public Money(decimal amount, string currencyCode, CurrencyKind currencyKind)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "The amount cannot be negative.");
        }

        if (!Enum.IsDefined(currencyKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(currencyKind),
                currencyKind,
                "The currency kind is invalid.");
        }

        Amount = amount;
        CurrencyKind = currencyKind;
        CurrencyCode = NormalizeCurrencyCode(currencyCode, currencyKind);
    }

    public decimal Amount { get; private set; }

    public string CurrencyCode { get; private set; }

    public CurrencyKind CurrencyKind { get; private set; }

    private static string NormalizeCurrencyCode(string currencyCode, CurrencyKind currencyKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        var normalizedCode = currencyCode.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaximumCurrencyCodeLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currencyCode),
                normalizedCode.Length,
                $"The currency code cannot exceed {MaximumCurrencyCodeLength} characters.");
        }

        var isAsciiAlphaNumeric = normalizedCode.All(character =>
            character is >= 'A' and <= 'Z' or >= '0' and <= '9');

        if (!isAsciiAlphaNumeric)
        {
            throw new ArgumentException(
                "The currency code can contain only ASCII letters and digits.",
                nameof(currencyCode));
        }

        const int Iso4217CodeLength = 3;

        if (currencyKind == CurrencyKind.Iso4217 &&
            (normalizedCode.Length != Iso4217CodeLength || normalizedCode.Any(char.IsDigit)))
        {
            throw new ArgumentException(
                "An ISO 4217 currency code must contain exactly three ASCII letters.",
                nameof(currencyCode));
        }

        return normalizedCode;
    }
}
