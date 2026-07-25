using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Core.CashFlow;
using NekoSubscription.Core.Configuration;

namespace NekoSubscription.Console.Infrastructure;

public static class ConsoleFormatters
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void PrintJson<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        System.Console.WriteLine(json);
    }

    public static Money CreateMoney(decimal amount, string currencyCode)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();
        var kind = (normalized.Length == 3 && normalized.All(char.IsLetter))
            ? CurrencyKind.Iso4217
            : CurrencyKind.Custom;
        return new Money(amount, normalized, kind);
    }

    public static string FormatMoney(Money money)
    {
        var symbol = money.CurrencyCode.ToUpperInvariant() switch
        {
            "USD" => "$",
            "CNY" or "RMB" => "¥",
            "EUR" => "€",
            "GBP" => "£",
            "JPY" => "¥",
            "HKD" => "HK$",
            "TWD" => "NT$",
            _ => $"{money.CurrencyCode} "
        };
        return $"{symbol}{money.Amount:F2}";
    }

    public static string FormatCategoryMarkup(SubscriptionCategory category)
    {
        return category switch
        {
            SubscriptionCategory.Ordinary => "[blue]Ordinary[/]",
            SubscriptionCategory.CloudService => "[purple]Cloud Service[/]",
            SubscriptionCategory.Domain => "[green]Domain[/]",
            SubscriptionCategory.PhoneNumber => "[cyan]Phone Number[/]",
            SubscriptionCategory.Custom => "[yellow]Custom[/]",
            _ => category.ToString()
        };
    }

    public static string FormatStatusMarkup(SubscriptionLifecycleStatus status)
    {
        return status switch
        {
            SubscriptionLifecycleStatus.Active => "[green]Active[/]",
            SubscriptionLifecycleStatus.Paused => "[yellow]Paused[/]",
            SubscriptionLifecycleStatus.Expired => "[red]Expired[/]",
            SubscriptionLifecycleStatus.Cancelled => "[grey]Cancelled[/]",
            _ => status.ToString()
        };
    }

    public static string FormatImportanceMarkup(SubscriptionImportance importance)
    {
        return importance switch
        {
            SubscriptionImportance.Essential => "[bold red]Essential[/]",
            SubscriptionImportance.Important => "[bold orange1]Important[/]",
            SubscriptionImportance.Normal => "[yellow]Normal[/]",
            SubscriptionImportance.Low => "[grey]Low[/]",
            _ => importance.ToString()
        };
    }

    public static string FormatBillingSchedule(BillingSchedule schedule)
    {
        if (schedule.Cadence == BillingCadence.OneTime)
        {
            return $"One-Time ({schedule.StartsOn:yyyy-MM-dd})";
        }
        if (schedule.Cadence == BillingCadence.Manual)
        {
            return "Manual";
        }

        var intervalStr = schedule.IntervalCount == 1
            ? $"{schedule.IntervalUnit}"
            : $"{schedule.IntervalCount} {schedule.IntervalUnit}s";

        var nextDate = schedule.NextBillingOn.HasValue ? $" (Next: [bold]{schedule.NextBillingOn.Value:yyyy-MM-dd}[/])" : "";
        return $"Every {intervalStr}{nextDate}";
    }

    public static void RenderSubscriptionsTable(IReadOnlyList<Subscription> subscriptions)
    {
        if (subscriptions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No subscriptions found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Subscriptions[/]")
            .AddColumn(new TableColumn("[bold]ID[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Category[/]"))
            .AddColumn(new TableColumn("[bold]Provider / Service[/]"))
            .AddColumn(new TableColumn("[bold]Account[/]"))
            .AddColumn(new TableColumn("[bold]Billing[/]"))
            .AddColumn(new TableColumn("[bold]Schedule[/]"))
            .AddColumn(new TableColumn("[bold]Status[/]"))
            .AddColumn(new TableColumn("[bold]Tags[/]"));

        foreach (var sub in subscriptions)
        {
            var shortId = sub.Id.ToString()[..8];
            var providerService = string.IsNullOrWhiteSpace(sub.ServiceName)
                ? $"[bold]{Markup.Escape(sub.ProviderName)}[/]"
                : $"[bold]{Markup.Escape(sub.ProviderName)}[/] / {Markup.Escape(sub.ServiceName)}";
            if (!string.IsNullOrWhiteSpace(sub.PlanName))
            {
                providerService += $" [dim]({Markup.Escape(sub.PlanName)})[/]";
            }

            var account = Markup.Escape(sub.AccountName ?? "-");
            var amountStr = FormatMoney(sub.BillingAmount);
            var scheduleStr = FormatBillingSchedule(sub.BillingSchedule);
            var statusStr = FormatStatusMarkup(sub.LifecycleStatus);
            var tagsStr = sub.Tags.Count > 0
                ? string.Join(", ", sub.Tags.Select(t => $"[blue]#{Markup.Escape(t.Name)}[/]"))
                : "-";

            table.AddRow(
                $"[grey]{shortId}[/]",
                FormatCategoryMarkup(sub.Category),
                providerService,
                account,
                amountStr,
                scheduleStr,
                statusStr,
                tagsStr
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]Total: {subscriptions.Count} subscription(s)[/]");
    }

    public static void RenderSubscriptionDetails(Subscription sub)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow("[bold]ID:[/]", sub.Id.ToString());
        grid.AddRow("[bold]Category:[/]", FormatCategoryMarkup(sub.Category));
        grid.AddRow("[bold]Provider Name:[/]", Markup.Escape(sub.ProviderName));
        grid.AddRow("[bold]Service Name:[/]", Markup.Escape(sub.ServiceName));
        grid.AddRow("[bold]Plan Name:[/]", Markup.Escape(sub.PlanName ?? "-"));
        grid.AddRow("[bold]Account Name:[/]", Markup.Escape(sub.AccountName ?? "-"));
        grid.AddRow("[bold]Billing Amount:[/]", FormatMoney(sub.BillingAmount));
        grid.AddRow("[bold]Billing Cadence:[/]", sub.BillingSchedule.Cadence.ToString());
        grid.AddRow("[bold]Billing Schedule:[/]", FormatBillingSchedule(sub.BillingSchedule));
        grid.AddRow("[bold]Status:[/]", FormatStatusMarkup(sub.LifecycleStatus));
        grid.AddRow("[bold]Confirmation:[/]", sub.ConfirmationStatus.ToString());
        grid.AddRow("[bold]Importance:[/]", FormatImportanceMarkup(sub.Importance));
        grid.AddRow("[bold]Participates In Budget:[/]", sub.ParticipatesInBudget ? "[green]Yes[/]" : "[grey]No[/]");

        if (sub.PaymentProfile != null)
        {
            grid.AddRow("[bold]Payment Profile:[/]", $"{Markup.Escape(sub.PaymentProfile.DisplayName)} ({sub.PaymentProfile.Channel})");
        }

        if (sub.Tags.Count > 0)
        {
            grid.AddRow("[bold]Tags:[/]", string.Join(", ", sub.Tags.Select(t => $"[blue]#{Markup.Escape(t.Name)}[/]")));
        }

        switch (sub)
        {
            case CloudServiceSubscription cloud:
                grid.AddRow("[bold]Cloud Billing Mode:[/]", cloud.BillingMode.ToString());
                if (!string.IsNullOrWhiteSpace(cloud.TenantIdentifier)) grid.AddRow("[bold]Tenant ID:[/]", Markup.Escape(cloud.TenantIdentifier));
                if (!string.IsNullOrWhiteSpace(cloud.ProjectIdentifier)) grid.AddRow("[bold]Project ID:[/]", Markup.Escape(cloud.ProjectIdentifier));
                break;
            case DomainSubscription domain:
                grid.AddRow("[bold]Domain Name:[/]", Markup.Escape(domain.DomainName));
                if (domain.RegisteredOn.HasValue) grid.AddRow("[bold]Registered On:[/]", domain.RegisteredOn.Value.ToString("yyyy-MM-dd"));
                if (domain.ExpiresOn.HasValue) grid.AddRow("[bold]Expires On:[/]", domain.ExpiresOn.Value.ToString("yyyy-MM-dd"));
                break;
            case PhoneNumberSubscription phone:
                grid.AddRow("[bold]Phone Number:[/]", Markup.Escape(phone.PhoneNumber));
                grid.AddRow("[bold]Number Type:[/]", phone.PhoneNumberType.ToString());
                if (!string.IsNullOrWhiteSpace(phone.CarrierName)) grid.AddRow("[bold]Carrier:[/]", Markup.Escape(phone.CarrierName));
                grid.AddRow("[bold]Prepaid:[/]", phone.IsPrepaid ? "[green]Yes[/]" : "[grey]No[/]");
                break;
            case CustomSubscription custom:
                if (custom.Fields.Count > 0)
                {
                    var cfStr = string.Join("\n", custom.Fields.Select(cf =>
                    {
                        var val = cf.TextValue ?? cf.NumberValue?.ToString() ?? cf.BooleanValue?.ToString() ?? cf.DateValue?.ToString() ?? cf.UrlValue ?? "";
                        return $"  • {Markup.Escape(cf.Name)} ({cf.FieldType}): {Markup.Escape(val)}";
                    }));
                    grid.AddRow("[bold]Custom Fields:[/]", cfStr);
                }
                break;
        }

        if (sub.PaymentDeferralPolicy != null)
        {
            grid.AddRow("[bold]Grace Period Days:[/]", sub.PaymentDeferralPolicy.ProviderGracePeriodDays?.ToString() ?? "-");
            grid.AddRow("[bold]Tolerance Days:[/]", sub.PaymentDeferralPolicy.BudgetToleranceDays?.ToString() ?? "-");
        }

        if (!string.IsNullOrWhiteSpace(sub.Notes))
        {
            grid.AddRow("[bold]Notes:[/]", Markup.Escape(sub.Notes));
        }

        var panel = new Panel(grid)
        {
            Header = new PanelHeader($"[bold blue]Subscription Details: {Markup.Escape(sub.ProviderName)} - {Markup.Escape(sub.ServiceName)}[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 1, 1, 1)
        };

        AnsiConsole.Write(panel);
    }

    public static void RenderPaymentProfilesTable(IReadOnlyList<PaymentProfile> profiles)
    {
        if (profiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No payment profiles found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Payment Profiles[/]")
            .AddColumn(new TableColumn("[bold]ID[/]"))
            .AddColumn(new TableColumn("[bold]Display Name[/]"))
            .AddColumn(new TableColumn("[bold]Channel[/]"))
            .AddColumn(new TableColumn("[bold]Provider[/]"))
            .AddColumn(new TableColumn("[bold]Account Identifier[/]"))
            .AddColumn(new TableColumn("[bold]Status[/]"));

        foreach (var p in profiles)
        {
            var shortId = p.Id.ToString()[..8];
            var statusStr = p.IsArchived ? "[grey]Archived[/]" : "[green]Active[/]";
            table.AddRow(
                $"[grey]{shortId}[/]",
                Markup.Escape(p.DisplayName),
                p.Channel.ToString(),
                Markup.Escape(p.ProviderName ?? "-"),
                Markup.Escape(p.AccountIdentifier ?? "-"),
                statusStr
            );
        }

        AnsiConsole.Write(table);
    }

    public static void RenderTagsTable(IReadOnlyList<Tag> tags)
    {
        if (tags.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No tags found.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Tags[/]")
            .AddColumn(new TableColumn("[bold]ID[/]"))
            .AddColumn(new TableColumn("[bold]Name[/]"));

        foreach (var tag in tags)
        {
            table.AddRow(
                $"[grey]{tag.Id.ToString()[..8]}[/]",
                $"[blue]#{Markup.Escape(tag.Name)}[/]"
            );
        }

        AnsiConsole.Write(table);
    }

    public static void RenderCashFlow(CashFlowProjection projection)
    {
        AnsiConsole.MarkupLine($"[bold blue]Cash Flow Projection ({projection.StartsOn:yyyy-MM-dd} to {projection.EndsOn:yyyy-MM-dd})[/]");

        if (projection.Items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No scheduled payments found in this date range.[/]");
            return;
        }

        var itemsTable = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Scheduled Payments[/]")
            .AddColumn(new TableColumn("[bold]Scheduled Date[/]"))
            .AddColumn(new TableColumn("[bold]Provider / Service[/]"))
            .AddColumn(new TableColumn("[bold]Account[/]"))
            .AddColumn(new TableColumn("[bold]Amount[/]"))
            .AddColumn(new TableColumn("[bold]Importance[/]"));

        foreach (var item in projection.Items)
        {
            var providerStr = string.IsNullOrWhiteSpace(item.ServiceName)
                ? Markup.Escape(item.ProviderName)
                : $"{Markup.Escape(item.ProviderName)} / {Markup.Escape(item.ServiceName)}";
            var estStr = item.IsEstimate ? " [dim](Estimate)[/]" : "";
            var amountStr = $"{FormatMoney(item.Amount)}{estStr}";

            itemsTable.AddRow(
                item.ScheduledOn.ToString("yyyy-MM-dd"),
                providerStr,
                Markup.Escape(item.AccountName ?? "-"),
                amountStr,
                FormatImportanceMarkup(item.Importance)
            );
        }

        AnsiConsole.Write(itemsTable);

        if (projection.CurrencyTotals.Count > 0)
        {
            var totalsTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Totals by Currency[/]")
                .AddColumn(new TableColumn("[bold]Currency[/]"))
                .AddColumn(new TableColumn("[bold]Fixed Amount Total[/]"))
                .AddColumn(new TableColumn("[bold]Estimated Amount Total[/]"))
                .AddColumn(new TableColumn("[bold]Grand Total[/]"));

            foreach (var total in projection.CurrencyTotals)
            {
                var fixedStr = $"{total.CurrencyCode} {total.FixedAmount:F2}";
                var estStr = $"{total.CurrencyCode} {total.EstimatedAmount:F2}";
                var grandStr = $"{total.CurrencyCode} {total.TotalAmount:F2}";

                totalsTable.AddRow(
                    total.CurrencyCode,
                    fixedStr,
                    estStr,
                    $"[bold green]{grandStr}[/]"
                );
            }

            AnsiConsole.Write(totalsTable);
        }
    }

    public static void RenderSettings(ApplicationSettings settings)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Application Settings[/]")
            .AddColumn(new TableColumn("[bold]Setting[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        table.AddRow("Theme", settings.Theme.ToString());
        table.AddRow("Minimum Log Level", settings.MinimumLogLevel.ToString());
        table.AddRow("Visual Style", settings.VisualStyle.ToString());

        AnsiConsole.Write(table);
    }
}
