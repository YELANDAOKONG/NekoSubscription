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

public sealed class ProfileListCommand : AsyncCommand<ProfileListCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("--archived")]
        [Description("Include archived profiles.")]
        public bool IncludeArchived { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var profiles = await runtime.Subscriptions.GetPaymentProfilesAsync(settings.IncludeArchived, cancellationToken);

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(profiles);
            return 0;
        }

        ConsoleFormatters.RenderPaymentProfilesTable(profiles);
        return 0;
    }
}

public sealed class ProfileAddCommand : AsyncCommand<ProfileAddCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("-n|--name <NAME>")]
        public string? DisplayName { get; init; }

        [CommandOption("-c|--channel <CHANNEL>")]
        public PaymentChannel? Channel { get; init; }

        [CommandOption("-a|--account <ACCOUNT>")]
        public string? AccountIdentifier { get; init; }

        [CommandOption("-p|--provider <PROVIDER>")]
        public string? ProviderName { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);

        var name = settings.DisplayName ?? AnsiConsole.Ask<string>("Enter [green]Profile Display Name[/] (e.g. Visa 1234, Apple Pay):");
        var channel = settings.Channel ?? AnsiConsole.Prompt(
            new SelectionPrompt<PaymentChannel>()
                .Title("Select [green]Payment Channel[/]:")
                .AddChoices(Enum.GetValues<PaymentChannel>()));

        var accountId = settings.AccountIdentifier;
        if (string.IsNullOrWhiteSpace(accountId) && (channel == PaymentChannel.AppleAppStore || channel == PaymentChannel.GooglePlay || channel == PaymentChannel.PayPal))
        {
            accountId = AnsiConsole.Ask<string>($"Enter [green]Account Identifier[/] (required for {channel}):");
        }

        var profile = new PaymentProfile(name, channel, accountId, settings.ProviderName, null);
        await runtime.Subscriptions.AddPaymentProfileAsync(profile, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Successfully added payment profile:[/] [bold]{Markup.Escape(profile.DisplayName)}[/] (ID: [grey]{profile.Id}[/])");
        return 0;
    }
}

public sealed class ProfileArchiveCommand : AsyncCommand<ProfileArchiveCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        public string Id { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var profiles = await runtime.Subscriptions.GetPaymentProfilesAsync(includeArchived: true, cancellationToken: cancellationToken);
        var match = profiles.FirstOrDefault(p =>
            p.Id.ToString().Equals(settings.Id, StringComparison.OrdinalIgnoreCase) ||
            p.Id.ToString().StartsWith(settings.Id, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Payment profile with ID '[bold]{settings.Id}[/]' was not found.");
            return 1;
        }

        var success = await runtime.Subscriptions.ArchivePaymentProfileAsync(match.Id, cancellationToken);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Successfully archived payment profile [bold]{match.DisplayName}[/][/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Failed to archive payment profile.[/]");
        return 1;
    }
}

public sealed class ProfileRestoreCommand : AsyncCommand<ProfileRestoreCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        public string Id { get; init; } = string.Empty;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var profiles = await runtime.Subscriptions.GetPaymentProfilesAsync(includeArchived: true, cancellationToken: cancellationToken);
        var match = profiles.FirstOrDefault(p =>
            p.Id.ToString().Equals(settings.Id, StringComparison.OrdinalIgnoreCase) ||
            p.Id.ToString().StartsWith(settings.Id, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Payment profile with ID '[bold]{settings.Id}[/]' was not found.");
            return 1;
        }

        var success = await runtime.Subscriptions.RestorePaymentProfileFromArchiveAsync(match.Id, cancellationToken);
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Successfully restored payment profile [bold]{match.DisplayName}[/][/]");
            return 0;
        }

        AnsiConsole.MarkupLine("[red]Failed to restore payment profile.[/]");
        return 1;
    }
}
