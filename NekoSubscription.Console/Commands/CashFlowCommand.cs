using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;
using NekoSubscription.Core.CashFlow;

namespace NekoSubscription.Console.Commands;

public sealed class CashFlowCommand : AsyncCommand<CashFlowCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("--days <DAYS>")]
        [Description("Number of forecast days starting from today. Default is 30.")]
        public int Days { get; init; } = 30;

        [CommandOption("--start <DATE>")]
        [Description("Forecast start date (yyyy-MM-dd).")]
        public string? StartDate { get; init; }

        [CommandOption("--end <DATE>")]
        [Description("Forecast end date (yyyy-MM-dd).")]
        public string? EndDate { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly startsOn = today;
        DateOnly endsOn = today.AddDays(settings.Days);

        if (!string.IsNullOrWhiteSpace(settings.StartDate) && DateOnly.TryParse(settings.StartDate, out var parsedStart))
        {
            startsOn = parsedStart;
        }

        if (!string.IsNullOrWhiteSpace(settings.EndDate) && DateOnly.TryParse(settings.EndDate, out var parsedEnd))
        {
            endsOn = parsedEnd;
        }

        var subscriptions = await runtime.Subscriptions.GetSubscriptionsAsync(cancellationToken: cancellationToken);
        var projector = new CashFlowProjector();
        var projection = projector.Project(subscriptions, startsOn, endsOn);

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(projection);
            return 0;
        }

        ConsoleFormatters.RenderCashFlow(projection);
        return 0;
    }
}
