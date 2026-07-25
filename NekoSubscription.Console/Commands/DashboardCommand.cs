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

public sealed class DashboardCommand : AsyncCommand<GlobalCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GlobalCommandSettings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var subscriptions = await runtime.Subscriptions.GetSubscriptionsAsync(cancellationToken: cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var thirtyDaysLater = today.AddDays(30);

        var activeSubs = subscriptions.Where(s => s.LifecycleStatus == SubscriptionLifecycleStatus.Active).ToList();
        var projector = new CashFlowProjector();
        var projection = projector.Project(subscriptions, today, thirtyDaysLater);

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(new
            {
                TotalSubscriptions = subscriptions.Count,
                ActiveSubscriptions = activeSubs.Count,
                UpcomingPaymentsCount = projection.Items.Count,
                CurrencyTotals = projection.CurrencyTotals.Select(c => new { c.CurrencyCode, c.FixedAmount, c.EstimatedAmount }),
                CategoryBreakdown = subscriptions.GroupBy(s => s.Category.ToString()).ToDictionary(g => g.Key, g => g.Count())
            });
            return 0;
        }

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            new Panel($"[bold green]{subscriptions.Count}[/]\n[dim]Total Subscriptions[/]") { Border = BoxBorder.Rounded, Padding = new Padding(1, 0, 1, 0) },
            new Panel($"[bold blue]{activeSubs.Count}[/]\n[dim]Active[/]") { Border = BoxBorder.Rounded, Padding = new Padding(1, 0, 1, 0) },
            new Panel($"[bold yellow]{projection.Items.Count}[/]\n[dim]Upcoming (30 Days)[/]") { Border = BoxBorder.Rounded, Padding = new Padding(1, 0, 1, 0) }
        );

        AnsiConsole.Write(new Panel(grid)
        {
            Header = new PanelHeader("[bold blue]NekoSubscription Overview[/]"),
            Border = BoxBorder.Double
        });

        ConsoleFormatters.RenderCashFlow(projection);

        return 0;
    }
}
