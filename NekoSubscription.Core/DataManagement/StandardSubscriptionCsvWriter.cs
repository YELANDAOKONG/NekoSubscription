using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Core.DataManagement;

internal static class StandardSubscriptionCsvWriter
{
    private const string Mask = "***";

    private static readonly string[] Headers =
    [
        "SERVICE NAME",
        "MEMBERSHIP NAME",
        "ACCOUNT IDENTIFIER",
        "PERIODIC FEE",
        "CURRENCY",
        "PAYMENT CYCLE",
        "EFFECTIVE DATE",
        "EXPIRATION DATE",
        "REMAINING VALIDITY",
        "SUBSCRIPTION MARKER",
        "PAYMENT METHOD",
        "PAYMENT ACCOUNT",
        "NOTES"
    ];

    public static async Task WriteAsync(
        Stream destination,
        IEnumerable<Subscription> subscriptions,
        bool maskAccountIdentifiers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(subscriptions);

        await using var writer = new StreamWriter(
            destination,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            leaveOpen: true)
        {
            NewLine = "\r\n"
        };

        await WriteRowAsync(writer, Headers, cancellationToken).ConfigureAwait(false);

        foreach (var subscription in subscriptions)
        {
            await WriteRowAsync(
                    writer,
                    CreateFields(subscription, maskAccountIdentifiers),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string[] CreateFields(
        Subscription subscription,
        bool maskAccountIdentifiers)
    {
        var paymentProfile = subscription.PaymentProfile;

        return
        [
            subscription.ProviderName,
            subscription.ServiceName,
            MaskIfRequested(subscription.AccountName, maskAccountIdentifiers),
            subscription.BillingAmount.Amount.ToString(
                "0.############################",
                CultureInfo.InvariantCulture),
            subscription.BillingAmount.CurrencyCode,
            FormatBillingInterval(subscription.BillingSchedule),
            FormatDate(subscription.BillingSchedule.StartsOn),
            FormatDate(subscription.BillingSchedule.NextBillingOn),
            string.Empty,
            subscription.ConfirmationStatus == SubscriptionConfirmationStatus.ConfirmedActive
                ? "TRUE"
                : "FALSE",
            FormatPaymentChannel(paymentProfile?.Channel ?? PaymentChannel.Direct),
            paymentProfile is null
                ? "-"
                : MaskIfRequested(paymentProfile.AccountIdentifier, maskAccountIdentifiers),
            subscription.Notes ?? string.Empty
        ];
    }

    private static string MaskIfRequested(string? value, bool maskAccountIdentifiers)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return maskAccountIdentifiers ? Mask : value;
    }

    private static string FormatBillingInterval(BillingSchedule schedule)
    {
        if (schedule.Cadence != BillingCadence.Recurring ||
            schedule.IntervalUnit is not { } intervalUnit ||
            schedule.IntervalCount is not { } intervalCount)
        {
            return string.Empty;
        }

        return (intervalUnit, intervalCount) switch
        {
            (BillingIntervalUnit.Day, 1) => "D",
            (BillingIntervalUnit.Week, 1) => "W",
            (BillingIntervalUnit.Month, 1) => "M",
            (BillingIntervalUnit.Month, 3) => "Q",
            (BillingIntervalUnit.Month, 6) => "HY",
            (BillingIntervalUnit.Year, 1) => "Y",
            (BillingIntervalUnit.Day, _) => $"{intervalCount.ToString(CultureInfo.InvariantCulture)}D",
            (BillingIntervalUnit.Week, _) => $"{intervalCount.ToString(CultureInfo.InvariantCulture)}W",
            (BillingIntervalUnit.Month, _) => $"{intervalCount.ToString(CultureInfo.InvariantCulture)}M",
            (BillingIntervalUnit.Year, _) => $"{intervalCount.ToString(CultureInfo.InvariantCulture)}Y",
            _ => throw new ArgumentOutOfRangeException(
                nameof(schedule),
                intervalUnit,
                "The billing interval unit is invalid.")
        };
    }

    private static string FormatDate(DateOnly? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatPaymentChannel(PaymentChannel channel) => channel switch
    {
        PaymentChannel.Direct => "DIRECT",
        PaymentChannel.AppleAppStore => "APPLE",
        PaymentChannel.GooglePlay => "GOOGLE",
        PaymentChannel.PayPal => "PAYPAL",
        PaymentChannel.BankTransfer => "BANK",
        PaymentChannel.CreditCard => "CREDIT_CARD",
        PaymentChannel.DebitCard => "DEBIT_CARD",
        PaymentChannel.Cash => "CASH",
        PaymentChannel.Other => "OTHER",
        _ => throw new ArgumentOutOfRangeException(
            nameof(channel),
            channel,
            "The payment channel is invalid.")
    };

    private static async Task WriteRowAsync(
        TextWriter writer,
        IEnumerable<string> fields,
        CancellationToken cancellationToken)
    {
        var row = string.Join(',', fields.Select(EscapeField));
        await writer.WriteLineAsync(row.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeField(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
