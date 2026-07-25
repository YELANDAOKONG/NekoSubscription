using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Console.Commands;

public sealed class TagListCommand : AsyncCommand<GlobalCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GlobalCommandSettings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var tags = await runtime.Subscriptions.GetTagsAsync(cancellationToken);

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(tags);
            return 0;
        }

        ConsoleFormatters.RenderTagsTable(tags);
        return 0;
    }
}

public sealed class TagAddCommand : AsyncCommand<TagAddCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<NAME>")]
        [Description("The tag name.")]
        public string Name { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var name = settings.Name.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(name))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Tag name cannot be empty.");
            return 1;
        }

        var tag = new Tag(name);
        await runtime.Subscriptions.AddTagAsync(tag, cancellationToken);
        AnsiConsole.MarkupLine($"[green]Tag added:[/] [blue]#{Markup.Escape(tag.Name)}[/] (ID: [grey]{tag.Id}[/])");
        return 0;
    }
}

public sealed class TagRenameCommand : AsyncCommand<TagRenameCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        [Description("The tag ID or prefix.")]
        public string Id { get; init; } = string.Empty;

        [CommandArgument(1, "<NEW_NAME>")]
        [Description("The new tag name.")]
        public string NewName { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var tags = await runtime.Subscriptions.GetTagsAsync(cancellationToken);
        var match = tags.FirstOrDefault(t =>
            t.Id.ToString().Equals(settings.Id, StringComparison.OrdinalIgnoreCase) ||
            t.Id.ToString().StartsWith(settings.Id, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Tag with ID '[bold]{settings.Id}[/]' was not found.");
            return 1;
        }

        var newName = settings.NewName.Trim().TrimStart('#');
        var success = await runtime.Subscriptions.RenameTagAsync(match.Id, newName, cancellationToken);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Tag renamed to:[/] [blue]#{Markup.Escape(newName)}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Failed to rename tag.[/]");
        return 1;
    }
}
