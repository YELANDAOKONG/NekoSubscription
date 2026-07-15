using System;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Entities.Tests.Subscriptions;

public sealed class CustomFieldTests
{
    [Fact]
    public void Factories_SetOnlyMatchingTypedValue()
    {
        var textField = CustomField.CreateText("Environment", "Production");
        var numberField = CustomField.CreateNumber("Instances", 3m);
        var booleanField = CustomField.CreateBoolean("Managed", true);
        var dateField = CustomField.CreateDate("Renewal", new DateOnly(2026, 12, 1));
        var urlField = CustomField.CreateUrl("Portal", "https://example.com/portal");

        Assert.Equal("Production", textField.TextValue);
        Assert.Null(textField.NumberValue);
        Assert.Equal(3m, numberField.NumberValue);
        Assert.True(booleanField.BooleanValue);
        Assert.Equal(new DateOnly(2026, 12, 1), dateField.DateValue);
        Assert.Equal("https://example.com/portal", urlField.UrlValue);
    }

    [Fact]
    public void CreateUrl_RejectsRelativeUrl()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CustomField.CreateUrl("Portal", "/relative/path"));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void CreateText_RejectsNegativeSortOrder()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => CustomField.CreateText("Environment", "Production", -1));

        Assert.Equal("sortOrder", exception.ParamName);
    }
}
