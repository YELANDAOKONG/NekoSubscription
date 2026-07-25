using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;
using NekoSubscription.Core.Subscriptions;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Console.Commands;

public sealed class EditCommand : AsyncCommand<EditCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandArgument(0, "<ID>")]
        [Description("The GUID or short ID prefix of the subscription.")]
        public string Id { get; init; } = string.Empty;

        [CommandOption("-p|--provider <PROVIDER>")]
        public string? ProviderName { get; init; }

        [CommandOption("-s|--service <SERVICE>")]
        public string? ServiceName { get; init; }

        [CommandOption("--plan <PLAN>")]
        public string? PlanName { get; init; }

        [CommandOption("-a|--account <ACCOUNT>")]
        public string? AccountName { get; init; }

        [CommandOption("--amount <AMOUNT>")]
        public decimal? Amount { get; init; }

        [CommandOption("--currency <CURRENCY>")]
        public string? CurrencyCode { get; init; }

        [CommandOption("--status <STATUS>")]
        public SubscriptionLifecycleStatus? Status { get; init; }

        [CommandOption("--notes <NOTES>")]
        public string? Notes { get; init; }
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

        var hasFlags = settings.ProviderName != null || settings.ServiceName != null || settings.PlanName != null ||
                       settings.AccountName != null || settings.Amount != null || settings.CurrencyCode != null ||
                       settings.Status != null || settings.Notes != null;

        var now = DateTimeOffset.UtcNow;

        if (hasFlags)
        {
            await runtime.Subscriptions.UpdateSubscriptionAsync(sub.Id, (s, ts) =>
            {
                var newProvider = settings.ProviderName ?? s.ProviderName;
                var newService = settings.ServiceName ?? s.ServiceName;
                var newPlan = settings.PlanName ?? s.PlanName;
                var newAccount = settings.AccountName ?? s.AccountName;
                s.UpdateIdentity(newProvider, newService, newPlan, newAccount, ts);

                if (settings.Amount.HasValue || settings.CurrencyCode != null)
                {
                    var newAmount = settings.Amount ?? s.BillingAmount.Amount;
                    var newCurrency = (settings.CurrencyCode ?? s.BillingAmount.CurrencyCode).ToUpperInvariant();
                    s.UpdateBilling(ConsoleFormatters.CreateMoney(newAmount, newCurrency), s.BillingSchedule, ts);
                }

                if (settings.Status.HasValue)
                {
                    s.SetStatuses(s.ConfirmationStatus, settings.Status.Value, ts);
                }

                if (settings.Notes != null)
                {
                    s.UpdateNotesAndManagementUrl(settings.Notes, s.ManagementUrl, ts);
                }
            }, cancellationToken);
        }
        else
        {
            AnsiConsole.MarkupLine($"[bold blue]Editing Subscription:[/] {Markup.Escape(sub.ProviderName)} - {Markup.Escape(sub.ServiceName)}");

            var newProvider = AnsiConsole.Ask<string>("Provider Name:", sub.ProviderName);
            var newService = AnsiConsole.Ask<string>("Service Name:", sub.ServiceName);
            var newPlan = AnsiConsole.Ask<string>("Plan Name (blank to skip):", sub.PlanName ?? "");
            var newAccount = AnsiConsole.Ask<string>("Account Name (blank to skip):", sub.AccountName ?? "");
            var newAmount = AnsiConsole.Ask<decimal>("Billing Amount:", sub.BillingAmount.Amount);
            var newCurrency = AnsiConsole.Ask<string>("Currency Code:", sub.BillingAmount.CurrencyCode).ToUpperInvariant();

            var newStatus = AnsiConsole.Prompt(
                new SelectionPrompt<SubscriptionLifecycleStatus>()
                    .Title("Lifecycle Status:")
                    .AddChoices(
                        SubscriptionLifecycleStatus.Active,
                        SubscriptionLifecycleStatus.Paused,
                        SubscriptionLifecycleStatus.Expired,
                        SubscriptionLifecycleStatus.Cancelled));

            var newNotes = AnsiConsole.Ask<string>("Notes (blank to skip):", sub.Notes ?? "");

            await runtime.Subscriptions.UpdateSubscriptionAsync(sub.Id, (s, ts) =>
            {
                s.UpdateIdentity(newProvider, newService, string.IsNullOrWhiteSpace(newPlan) ? null : newPlan, string.IsNullOrWhiteSpace(newAccount) ? null : newAccount, ts);
                s.UpdateBilling(ConsoleFormatters.CreateMoney(newAmount, newCurrency), s.BillingSchedule, ts);
                s.SetStatuses(s.ConfirmationStatus, newStatus, ts);
                s.UpdateNotesAndManagementUrl(string.IsNullOrWhiteSpace(newNotes) ? null : newNotes, s.ManagementUrl, ts);
            }, cancellationToken);
        }

        AnsiConsole.MarkupLine($"[green]Successfully updated subscription [bold]{sub.Id}[/][/]");
        return 0;
    }
}
