using System;

namespace NekoSubscription.Entities.Subscriptions;

public sealed class CustomField
{
    public const int MaximumNameLength = 100;
    public const int MaximumTextValueLength = 4000;
    public const int MaximumUrlValueLength = 2048;

    private CustomField()
    {
        Name = string.Empty;
    }

    private CustomField(string name, CustomFieldType fieldType, int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, "The sort order cannot be negative.");
        }

        Id = Guid.NewGuid();
        Name = NormalizeRequired(name, MaximumNameLength, nameof(name));
        FieldType = fieldType;
        SortOrder = sortOrder;
    }

    public Guid Id { get; private set; }

    public Guid CustomSubscriptionId { get; private set; }

    public CustomSubscription CustomSubscription { get; private set; } = null!;

    public string Name { get; private set; }

    public CustomFieldType FieldType { get; private set; }

    public int SortOrder { get; private set; }

    public string? TextValue { get; private set; }

    public decimal? NumberValue { get; private set; }

    public bool? BooleanValue { get; private set; }

    public DateOnly? DateValue { get; private set; }

    public string? UrlValue { get; private set; }

    public static CustomField CreateText(string name, string value, int sortOrder = 0)
    {
        var field = new CustomField(name, CustomFieldType.Text, sortOrder)
        {
            TextValue = NormalizeRequired(value, MaximumTextValueLength, nameof(value))
        };

        return field;
    }

    public static CustomField CreateNumber(string name, decimal value, int sortOrder = 0)
    {
        return new CustomField(name, CustomFieldType.Number, sortOrder)
        {
            NumberValue = value
        };
    }

    public static CustomField CreateBoolean(string name, bool value, int sortOrder = 0)
    {
        return new CustomField(name, CustomFieldType.Boolean, sortOrder)
        {
            BooleanValue = value
        };
    }

    public static CustomField CreateDate(string name, DateOnly value, int sortOrder = 0)
    {
        return new CustomField(name, CustomFieldType.Date, sortOrder)
        {
            DateValue = value
        };
    }

    public static CustomField CreateUrl(string name, string value, int sortOrder = 0)
    {
        var normalizedUrl = NormalizeRequired(value, MaximumUrlValueLength, nameof(value));

        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("The custom field URL must be absolute.", nameof(value));
        }

        return new CustomField(name, CustomFieldType.Url, sortOrder)
        {
            UrlValue = normalizedUrl
        };
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
}
