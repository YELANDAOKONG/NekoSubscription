using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;
using NekoSubscription.Core.CashFlow;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Console.Commands;

public sealed class DashboardCommand : AsyncCommand<DashboardCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("--days <DAYS>")]
        [Description("Forecast period in days (e.g. 3, 7, 14, 30, 90). Default is 7.")]
        public int Days { get; init; } = 7;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var subscriptions = await runtime.Subscriptions.GetSubscriptionsAsync(cancellationToken: cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dayCount = settings.Days > 0 ? settings.Days : 7;
        var projectionEndsOn = today.AddDays(dayCount - 1);

        var visibleSubscriptions = subscriptions
            .Where(s => !s.IsArchived && !s.IsDeleted)
            .ToList();

        var activeCount = visibleSubscriptions.Count(s =>
            s.LifecycleStatus is SubscriptionLifecycleStatus.Active or SubscriptionLifecycleStatus.Trial);
        var archivedCount = subscriptions.Count(s => s.IsArchived);
        var trialCount = visibleSubscriptions.Count(s => s.LifecycleStatus == SubscriptionLifecycleStatus.Trial);
        var excludedCount = visibleSubscriptions.Count(s => !s.ParticipatesInBudget);

        // Calculate Overdue Payments (same logic as Desktop DashboardViewModel)
        var overduePayments = visibleSubscriptions
            .Where(s => s.ParticipatesInBudget)
            .Select(s => new
            {
                Subscription = s,
                DueOn = GetRecordedDueOnForOverdue(s)
            })
            .Where(item => item.DueOn is { } dueOn && dueOn < today)
            .OrderBy(item => item.DueOn)
            .Select(item => new
            {
                item.Subscription,
                DueOn = item.DueOn!.Value,
                DaysOverdue = today.DayNumber - item.DueOn!.Value.DayNumber
            })
            .ToList();

        var projector = new CashFlowProjector();
        var projection = projector.Project(visibleSubscriptions, today, projectionEndsOn);

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(new
            {
                ForecastDays = dayCount,
                TotalSubscriptions = subscriptions.Count,
                ActiveSubscriptions = activeCount,
                ArchivedSubscriptions = archivedCount,
                TrialSubscriptions = trialCount,
                ExcludedSubscriptions = excludedCount,
                OverdueCount = overduePayments.Count,
                OverduePayments = overduePayments.Select(o => new
                {
                    o.Subscription.Id,
                    o.Subscription.ProviderName,
                    o.Subscription.ServiceName,
                    DueOn = o.DueOn.ToString("yyyy-MM-dd"),
                    o.DaysOverdue,
                    Amount = new { o.Subscription.BillingAmount.Amount, o.Subscription.BillingAmount.CurrencyCode }
                }),
                UpcomingPaymentsCount = projection.Items.Count,
                CurrencyTotals = projection.CurrencyTotals.Select(c => new { c.CurrencyCode, c.FixedAmount, c.EstimatedAmount, c.TotalAmount }),
                CategoryBreakdown = subscriptions.GroupBy(s => s.Category.ToString()).ToDictionary(g => g.Key, g => g.Count())
            });
            return 0;
        }

        // Render Overview Banner Grid
        var summaryGrid = new Grid();
        summaryGrid.AddColumn();
        summaryGrid.AddColumn();
        summaryGrid.AddColumn();
        summaryGrid.AddColumn();

        summaryGrid.AddRow(
            new Panel($"[bold green]{activeCount}[/]\n[dim]Active[/]") { Border = BoxBorder.Rounded, Padding = new Padding(1, 0, 1, 0) },
            new Panel($"[bold yellow]{overduePayments.Count}[/]\n[dim]Overdue[/]") { Border = BoxBorder.Rounded, Padding = new Padding(1, 0, 1, 0) },
            new Panel($"[bold blue]{projection.Items.Count}[/]\n[dim]Forecast ({dayCount} Days)[/]") { Border = BoxBorder.Rounded, Padding = new Padding(1, 0, 1, 0) },
            new Panel($"[bold grey]{archivedCount}[/]\n[dim]Archived[/]") { Border = BoxBorder.Rounded, Padding = new Padding(1, 0, 1, 0) }
        );

        AnsiConsole.Write(new Panel(summaryGrid)
        {
            Header = new PanelHeader($"[bold blue]Dashboard Overview ({dayCount}-Day Projection)[/]"),
            Border = BoxBorder.Double
        });

        // Independent Overdue Section (if any overdue payments exist)
        if (overduePayments.Count > 0)
        {
            var overdueTable = new Table()
                .Border(TableBorder.Rounded)
                .Title($"[bold red]⚠️ Overdue Payments ({overduePayments.Count})[/]")
                .AddColumn(new TableColumn("[bold red]Due Date[/]"))
                .AddColumn(new TableColumn("[bold red]Days Overdue[/]"))
                .AddColumn(new TableColumn("[bold]Provider / Service[/]"))
                .AddColumn(new TableColumn("[bold]Account[/]"))
                .AddColumn(new TableColumn("[bold]Billing Amount[/]"));

            foreach (var item in overduePayments)
            {
                var providerStr = string.IsNullOrWhiteSpace(item.Subscription.ServiceName)
                    ? Markup.Escape(item.Subscription.ProviderName)
                    : $"{Markup.Escape(item.Subscription.ProviderName)} / {Markup.Escape(item.Subscription.ServiceName)}";

                overdueTable.AddRow(
                    $"[red]{item.DueOn:yyyy-MM-dd}[/]",
                    $"[bold red]{item.DaysOverdue} day(s) overdue[/]",
                    providerStr,
                    Markup.Escape(item.Subscription.AccountName ?? "-"),
                    ConsoleFormatters.FormatMoney(item.Subscription.BillingAmount)
                );
            }

            AnsiConsole.Write(overdueTable);
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.MarkupLine("[dim green]✓ No overdue payments.[/]\n");
        }

        // Render Cash Flow forecast table for the selected period
        ConsoleFormatters.RenderCashFlow(projection);

        return 0;
    }

    private static DateOnly? GetRecordedDueOnForOverdue(Subscription subscription)
    {
        if (subscription.BillingSchedule.NextBillingOn is { } nextBillingOn)
        {
            return nextBillingOn;
        }

        return subscription.BillingSchedule.Cadence is BillingCadence.OneTime or BillingCadence.Manual
            ? subscription.BillingSchedule.StartsOn
            : null;
    }
}
