using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;
using NekoSubscription.Core.Subscriptions;

namespace NekoSubscription.Console.Commands;

public sealed class DeleteCommand : AsyncCommand<DeleteCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        [Description("The GUID or short ID prefix of the subscription to delete.")]
        public string Id { get; init; } = string.Empty;

        [CommandOption("-f|--force")]
        [Description("Skip confirmation prompt.")]
        public bool Force { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var all = await runtime.Subscriptions.GetSubscriptionsAsync(cancellationToken: cancellationToken);
        var sub = all.FirstOrDefault(s =>
            s.Id.ToString().Equals(settings.Id, StringComparison.OrdinalIgnoreCase) ||
            s.Id.ToString().StartsWith(settings.Id, StringComparison.OrdinalIgnoreCase));

        if (sub == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Subscription with ID '[bold]{settings.Id}[/]' was not found.");
            return 1;
        }

        if (!settings.Force && !AnsiConsole.Confirm($"Are you sure you want to soft delete subscription '[bold]{Markup.Escape(sub.ProviderName)} - {Markup.Escape(sub.ServiceName)}[/]'?"))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return 0;
        }

        var success = await runtime.Subscriptions.SoftDeleteSubscriptionAsync(sub.Id, cancellationToken);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Successfully deleted subscription [bold]{sub.Id}[/][/]");
            return 0;
        }
        
        AnsiConsole.MarkupLine("[red]Failed to delete subscription.[/]");
        return 1;
    }
}

public sealed class ArchiveCommand : AsyncCommand<ArchiveCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        [Description("The GUID or short ID prefix of the subscription to archive.")]
        public string Id { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var all = await runtime.Subscriptions.GetSubscriptionsAsync(cancellationToken: cancellationToken);
        var sub = all.FirstOrDefault(s =>
            s.Id.ToString().Equals(settings.Id, StringComparison.OrdinalIgnoreCase) ||
            s.Id.ToString().StartsWith(settings.Id, StringComparison.OrdinalIgnoreCase));

        if (sub == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Subscription with ID '[bold]{settings.Id}[/]' was not found.");
            return 1;
        }

        var success = await runtime.Subscriptions.ArchiveSubscriptionAsync(sub.Id, cancellationToken);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Successfully archived subscription [bold]{sub.Id}[/][/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Failed to archive subscription.[/]");
        return 1;
    }
}

public sealed class RestoreCommand : AsyncCommand<RestoreCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        [Description("The GUID or short ID prefix of the subscription to restore.")]
        public string Id { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var all = await runtime.Subscriptions.GetSubscriptionsAsync(new SubscriptionQuery(IncludeArchived: true, IncludeDeleted: true), cancellationToken);
        var sub = all.FirstOrDefault(s =>
            s.Id.ToString().Equals(settings.Id, StringComparison.OrdinalIgnoreCase) ||
            s.Id.ToString().StartsWith(settings.Id, StringComparison.OrdinalIgnoreCase));

        if (sub == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Subscription with ID '[bold]{settings.Id}[/]' was not found.");
            return 1;
        }

        bool success = false;
        if (sub.IsDeleted)
        {
            success = await runtime.Subscriptions.RestoreDeletedSubscriptionAsync(sub.Id, cancellationToken);
        }
        else if (sub.IsArchived)
        {
            success = await runtime.Subscriptions.RestoreSubscriptionFromArchiveAsync(sub.Id, cancellationToken);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Subscription is neither deleted nor archived.[/]");
            return 0;
        }

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Successfully restored subscription [bold]{sub.Id}[/][/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Failed to restore subscription.[/]");
        return 1;
    }
}
