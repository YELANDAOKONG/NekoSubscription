using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;
using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.Console.Commands;

public sealed class AddCommand : AsyncCommand<AddCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("-c|--category <CATEGORY>")]
        [Description("Category: Ordinary, CloudService, Domain, PhoneNumber, Custom.")]
        public SubscriptionCategory? Category { get; init; }

        [CommandOption("-p|--provider <PROVIDER>")]
        [Description("Provider name.")]
        public string? ProviderName { get; init; }

        [CommandOption("-s|--service <SERVICE>")]
        [Description("Service name.")]
        public string? ServiceName { get; init; }

        [CommandOption("--plan <PLAN>")]
        [Description("Plan name.")]
        public string? PlanName { get; init; }

        [CommandOption("-a|--account <ACCOUNT>")]
        [Description("Account identifier/email.")]
        public string? AccountName { get; init; }

        [CommandOption("--amount <AMOUNT>")]
        [Description("Billing amount.")]
        public decimal? Amount { get; init; }

        [CommandOption("--currency <CURRENCY>")]
        [Description("Currency code (e.g. USD, CNY, EUR). Default is USD.")]
        public string? CurrencyCode { get; init; }

        [CommandOption("--cadence <CADENCE>")]
        [Description("Billing cadence: Recurring, OneTime, Manual. Default is Recurring.")]
        public BillingCadence? Cadence { get; init; }

        [CommandOption("--unit <UNIT>")]
        [Description("Billing interval unit: Day, Week, Month, Year. Default is Month.")]
        public BillingIntervalUnit? IntervalUnit { get; init; }

        [CommandOption("--interval <COUNT>")]
        [Description("Billing interval count. Default is 1.")]
        public int? IntervalCount { get; init; }

        [CommandOption("--start-date <DATE>")]
        [Description("Starts on date (yyyy-MM-dd). Default is today.")]
        public string? StartsOnDate { get; init; }

        [CommandOption("--domain-name <DOMAIN>")]
        [Description("Domain name (required if category is Domain).")]
        public string? DomainName { get; init; }

        [CommandOption("--phone-number <PHONE>")]
        [Description("Phone number (required if category is PhoneNumber).")]
        public string? PhoneNumber { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);

        var category = settings.Category ?? AnsiConsole.Prompt(
            new SelectionPrompt<SubscriptionCategory>()
                .Title("Select [green]Subscription Category[/]:")
                .AddChoices(
                    SubscriptionCategory.Ordinary,
                    SubscriptionCategory.CloudService,
                    SubscriptionCategory.Domain,
                    SubscriptionCategory.PhoneNumber,
                    SubscriptionCategory.Custom));

        var providerName = settings.ProviderName;
        if (string.IsNullOrWhiteSpace(providerName))
        {
            providerName = AnsiConsole.Ask<string>("Enter [green]Provider Name[/] (e.g. Netflix, AWS, GoDaddy):");
        }

        var serviceName = settings.ServiceName;
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            serviceName = AnsiConsole.Ask<string>("Enter [green]Service Name[/] (e.g. Premium Plan, EC2):", providerName);
        }

        var planName = settings.PlanName;
        var accountName = settings.AccountName;

        var amount = settings.Amount ?? AnsiConsole.Ask<decimal>("Enter [green]Billing Amount[/]:");
        var currencyCode = (settings.CurrencyCode ?? AnsiConsole.Ask<string>("Enter [green]Currency Code[/] (e.g. USD, CNY):", "USD")).ToUpperInvariant();

        var money = ConsoleFormatters.CreateMoney(amount, currencyCode);

        var cadence = settings.Cadence ?? BillingCadence.Recurring;
        BillingSchedule schedule;

        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly startsOn = today;
        if (!string.IsNullOrWhiteSpace(settings.StartsOnDate) && DateOnly.TryParse(settings.StartsOnDate, out var parsedStart))
        {
            startsOn = parsedStart;
        }

        if (cadence == BillingCadence.OneTime)
        {
            schedule = new BillingSchedule(BillingCadence.OneTime, null, null, startsOn, startsOn, startsOn, false);
        }
        else if (cadence == BillingCadence.Manual)
        {
            schedule = new BillingSchedule(BillingCadence.Manual, null, null, null, null, null, false);
        }
        else
        {
            var intervalUnit = settings.IntervalUnit ?? BillingIntervalUnit.Month;
            var intervalCount = settings.IntervalCount ?? 1;
            schedule = new BillingSchedule(BillingCadence.Recurring, intervalUnit, intervalCount, startsOn, startsOn, null, true);
        }

        Subscription sub = category switch
        {
            SubscriptionCategory.CloudService => new CloudServiceSubscription(
                providerName, serviceName, planName, accountName, money, schedule, CloudBillingMode.Fixed, null, null),
            SubscriptionCategory.Domain => new DomainSubscription(
                providerName, serviceName, planName, accountName, money, schedule,
                settings.DomainName ?? (AnsiConsole.Ask<string>("Enter [green]Domain Name[/]:")), null, null),
            SubscriptionCategory.PhoneNumber => new PhoneNumberSubscription(
                providerName, serviceName, planName, accountName, money, schedule,
                settings.PhoneNumber ?? (AnsiConsole.Ask<string>("Enter [green]Phone Number[/]:")),
                PhoneNumberType.Mobile, providerName, null, false),
            SubscriptionCategory.Custom => new CustomSubscription(
                providerName, serviceName, planName, accountName, money, schedule),
            _ => new OrdinarySubscription(
                providerName, serviceName, planName, accountName, money, schedule)
        };

        sub.SetStatuses(SubscriptionConfirmationStatus.ConfirmedActive, SubscriptionLifecycleStatus.Active, DateTimeOffset.UtcNow);

        await runtime.Subscriptions.AddSubscriptionAsync(sub, cancellationToken);

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(sub);
            return 0;
        }

        AnsiConsole.MarkupLine($"[green]Successfully added subscription:[/] [bold]{Markup.Escape(sub.ProviderName)} - {Markup.Escape(sub.ServiceName)}[/] (ID: [grey]{sub.Id}[/])");
        return 0;
    }
}
