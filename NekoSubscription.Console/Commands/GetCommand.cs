using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;

namespace NekoSubscription.Console.Commands;

public sealed class GetCommand : AsyncCommand<GetCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        [Description("The GUID or short ID prefix of the subscription.")]
        public string Id { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var all = await runtime.Subscriptions.GetSubscriptionsAsync(cancellationToken: cancellationToken);
        var match = all.FirstOrDefault(s =>
            s.Id.ToString().Equals(settings.Id, StringComparison.OrdinalIgnoreCase) ||
            s.Id.ToString().StartsWith(settings.Id, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Subscription with ID '[bold]{settings.Id}[/]' was not found.");
            return 1;
        }

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(match);
            return 0;
        }

        ConsoleFormatters.RenderSubscriptionDetails(match);
        return 0;
    }
}
