using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Entities.Tests.Subscriptions;

public sealed class MoneyTests
{
    [Fact]
    public void Constructor_NormalizesIso4217Code()
    {
        var money = new Money(19.99m, " usd ", CurrencyKind.Iso4217);

        Assert.Equal(19.99m, money.Amount);
        Assert.Equal("USD", money.CurrencyCode);
        Assert.Equal(CurrencyKind.Iso4217, money.CurrencyKind);
    }

    [Fact]
    public void Constructor_AcceptsCustomCryptocurrencyCode()
    {
        var money = new Money(0.125m, "usdt", CurrencyKind.Custom);

        Assert.Equal("USDT", money.CurrencyCode);
        Assert.Equal(CurrencyKind.Custom, money.CurrencyKind);
    }

    [Fact]
    public void Constructor_RejectsNegativeAmount()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Money(-1m, "USD", CurrencyKind.Iso4217));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("12A")]
    public void Constructor_RejectsInvalidIso4217Code(string currencyCode)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Money(1m, currencyCode, CurrencyKind.Iso4217));

        Assert.Equal("currencyCode", exception.ParamName);
    }

    [Fact]
    public void Constructor_RejectsUnsupportedCurrencyCharacters()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new Money(1m, "BTC-TEST", CurrencyKind.Custom));

        Assert.Equal("currencyCode", exception.ParamName);
    }
}
