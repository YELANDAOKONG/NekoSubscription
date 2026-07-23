using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualBasic.FileIO;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.DataManagement;

internal static class StandardSubscriptionCsvParser
{
    public const int ColumnCount = 13;
    public const int MaximumFileSize = 10 * 1024 * 1024;

    private const string MissingCurrencyCode = "XXX";

    private static readonly string[] SupportedDateFormats =
    [
        "M/d/yyyy",
        "M/d/yy",
        "yyyy-M-d",
        "yyyy-MM-dd"
    ];

    public static (IReadOnlyList<ImportedSubscriptionRow> Rows, CsvImportPreview Preview) Parse(
        ReadOnlyMemory<byte> csvData)
    {
        using var stream = new MemoryStream(csvData.ToArray(), writable: false);
        using var parser = new TextFieldParser(stream, Encoding.UTF8, true)
        {
            HasFieldsEnclosedInQuotes = true,
            TextFieldType = FieldType.Delimited,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        var rows = new List<ImportedSubscriptionRow>();
        var issues = new List<CsvImportIssue>();
        var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var headerWasRead = false;
        var rowNumber = 0;
        var totalRowCount = 0;

        while (!parser.EndOfData)
        {
            string[]? fields;

            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException)
            {
                issues.Add(new CsvImportIssue(
                    Math.Max(rowNumber, 1),
                    CsvImportIssueSeverity.Error,
                    CsvImportIssueCode.MalformedCsv));
                break;
            }

            rowNumber++;
            if (fields is null || fields.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (!headerWasRead)
            {
                headerWasRead = true;
                continue;
            }

            totalRowCount++;
            if (fields.Length != ColumnCount)
            {
                issues.Add(new CsvImportIssue(
                    rowNumber,
                    CsvImportIssueSeverity.Error,
                    CsvImportIssueCode.InvalidColumnCount));
                continue;
            }

            if (!TryParseRow(fields, rowNumber, issues, out var importedRow))
            {
                continue;
            }

            var duplicateKey = CreateDuplicateKey(importedRow);
            if (!duplicateKeys.Add(duplicateKey))
            {
                issues.Add(new CsvImportIssue(
                    rowNumber,
                    CsvImportIssueSeverity.Warning,
                    CsvImportIssueCode.DuplicateRow));
            }

            rows.Add(importedRow);
        }

        return (
            rows,
            new CsvImportPreview(totalRowCount, rows.Count, issues));
    }

    private static bool TryParseRow(
        IReadOnlyList<string> fields,
        int rowNumber,
        ICollection<CsvImportIssue> issues,
        out ImportedSubscriptionRow importedRow)
    {
        importedRow = null!;
        var issueCount = issues.Count;
        var providerName = NormalizeOptional(fields[0]);
        if (providerName is null)
        {
            AddError(issues, rowNumber, CsvImportIssueCode.MissingProvider);
        }

        var serviceName = NormalizeOptional(fields[1]) ?? providerName;
        var accountName = NormalizeOptional(fields[2]);
        var billingAmount = ParseMoney(fields[3], fields[4], rowNumber, issues);
        var interval = ParseBillingInterval(fields[5], rowNumber, issues);
        var startsOn = ParseDate(fields[6], rowNumber, issues);
        var nextBillingOn = ParseDate(fields[7], rowNumber, issues);
        if (startsOn is not null && nextBillingOn is not null && nextBillingOn < startsOn)
        {
            AddError(issues, rowNumber, CsvImportIssueCode.InvalidDateOrder);
        }

        var isActive = ParseSubscriptionMarker(fields[9], rowNumber, issues);
        var paymentChannel = ParsePaymentChannel(fields[10], rowNumber, issues);
        var paymentAccount = NormalizeOptional(fields[11]);
        if (paymentAccount == "-")
        {
            paymentAccount = null;
        }

        if (paymentChannel is PaymentChannel.AppleAppStore or
                PaymentChannel.GooglePlay or
                PaymentChannel.PayPal &&
            paymentAccount is null)
        {
            AddError(issues, rowNumber, CsvImportIssueCode.MissingPaymentAccount);
        }

        if (issues.Count != issueCount ||
            providerName is null ||
            serviceName is null ||
            billingAmount is null ||
            interval is null ||
            isActive is null ||
            paymentChannel is null)
        {
            return false;
        }

        importedRow = new ImportedSubscriptionRow(
            providerName,
            serviceName,
            accountName,
            billingAmount,
            interval.Value.Unit,
            interval.Value.Count,
            startsOn,
            nextBillingOn,
            isActive.Value,
            paymentChannel.Value,
            paymentAccount,
            NormalizeOptional(fields[12]));
        return true;
    }

    private static Money? ParseMoney(
        string amountText,
        string currencyText,
        int rowNumber,
        ICollection<CsvImportIssue> issues)
    {
        var normalizedAmount = NormalizeOptional(amountText);
        var normalizedCurrency = NormalizeOptional(currencyText)?.ToUpperInvariant();
        if (normalizedAmount is null && normalizedCurrency is null)
        {
            return new Money(0m, MissingCurrencyCode, CurrencyKind.Iso4217);
        }

        if (normalizedAmount is null ||
            normalizedCurrency is null ||
            !decimal.TryParse(
                normalizedAmount,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount) ||
            amount < 0 ||
            normalizedCurrency.Length > Money.MaximumCurrencyCodeLength ||
            normalizedCurrency.Any(character => character is not (>= 'A' and <= 'Z' or >= '0' and <= '9')))
        {
            AddError(issues, rowNumber, CsvImportIssueCode.InvalidAmountOrCurrency);
            return null;
        }

        var currencyKind = normalizedCurrency.Length == 3 &&
            normalizedCurrency.All(character => character is >= 'A' and <= 'Z')
                ? CurrencyKind.Iso4217
                : CurrencyKind.Custom;

        try
        {
            return new Money(amount, normalizedCurrency, currencyKind);
        }
        catch (ArgumentException)
        {
            AddError(issues, rowNumber, CsvImportIssueCode.InvalidAmountOrCurrency);
            return null;
        }
    }

    private static (BillingIntervalUnit Unit, int Count)? ParseBillingInterval(
        string value,
        int rowNumber,
        ICollection<CsvImportIssue> issues)
    {
        var normalizedValue = NormalizeOptional(value)?.ToUpperInvariant();
        var interval = normalizedValue switch
        {
            "D" => (BillingIntervalUnit.Day, 1),
            "W" => (BillingIntervalUnit.Week, 1),
            "M" => (BillingIntervalUnit.Month, 1),
            "Q" => (BillingIntervalUnit.Month, 3),
            "HY" => (BillingIntervalUnit.Month, 6),
            "Y" => (BillingIntervalUnit.Year, 1),
            _ => ((BillingIntervalUnit Unit, int Count)?)null
        };

        if (interval is null)
        {
            AddError(issues, rowNumber, CsvImportIssueCode.InvalidBillingPeriod);
        }

        return interval;
    }

    private static DateOnly? ParseDate(
        string value,
        int rowNumber,
        ICollection<CsvImportIssue> issues)
    {
        var normalizedValue = NormalizeOptional(value);
        if (normalizedValue is null)
        {
            return null;
        }

        if (DateOnly.TryParseExact(
                normalizedValue,
                SupportedDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        AddError(issues, rowNumber, CsvImportIssueCode.InvalidDate);
        return null;
    }

    private static bool? ParseSubscriptionMarker(
        string value,
        int rowNumber,
        ICollection<CsvImportIssue> issues)
    {
        var normalizedValue = NormalizeOptional(value);
        if (bool.TryParse(normalizedValue, out var marker))
        {
            return marker;
        }

        if (normalizedValue == "1")
        {
            return true;
        }

        if (normalizedValue == "0")
        {
            return false;
        }

        AddError(issues, rowNumber, CsvImportIssueCode.InvalidSubscriptionMarker);
        return null;
    }

    private static PaymentChannel? ParsePaymentChannel(
        string value,
        int rowNumber,
        ICollection<CsvImportIssue> issues)
    {
        var normalizedValue = NormalizeOptional(value)?.ToUpperInvariant();
        var paymentChannel = normalizedValue switch
        {
            "DIRECT" => PaymentChannel.Direct,
            "APPLE" => PaymentChannel.AppleAppStore,
            "GOOGLE" => PaymentChannel.GooglePlay,
            "PAYPAL" => PaymentChannel.PayPal,
            "BANK" => PaymentChannel.BankTransfer,
            "CREDIT_CARD" => PaymentChannel.CreditCard,
            "DEBIT_CARD" => PaymentChannel.DebitCard,
            "CASH" => PaymentChannel.Cash,
            "OTHER" => PaymentChannel.Other,
            _ => (PaymentChannel?)null
        };

        if (paymentChannel is null)
        {
            AddError(issues, rowNumber, CsvImportIssueCode.InvalidPaymentChannel);
        }

        return paymentChannel;
    }

    private static string CreateDuplicateKey(ImportedSubscriptionRow row) =>
        string.Join(
            '\u001F',
            row.ProviderName,
            row.ServiceName,
            row.AccountName,
            row.BillingAmount.Amount.ToString(CultureInfo.InvariantCulture),
            row.BillingAmount.CurrencyCode,
            row.NextBillingOn?.ToString("O", CultureInfo.InvariantCulture));

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddError(
        ICollection<CsvImportIssue> issues,
        int rowNumber,
        CsvImportIssueCode code)
    {
        issues.Add(new CsvImportIssue(rowNumber, CsvImportIssueSeverity.Error, code));
    }
}
