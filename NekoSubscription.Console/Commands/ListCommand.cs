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

public sealed class ListCommand : AsyncCommand<ListCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("-c|--category <CATEGORY>")]
        [Description("Filter by category (Ordinary, CloudService, Domain, PhoneNumber, Custom)")]
        public SubscriptionCategory? Category { get; init; }

        [CommandOption("-s|--status <STATUS>")]
        [Description("Filter by lifecycle status (Active, Paused, Expired, Cancelled)")]
        public SubscriptionLifecycleStatus? Status { get; init; }

        [CommandOption("-q|--query <TEXT>")]
        [Description("Search provider, service, plan or account name.")]
        public string? Query { get; init; }

        [CommandOption("-t|--tag <TAG>")]
        [Description("Filter by tag name.")]
        public string? Tag { get; init; }

        [CommandOption("--archived")]
        [Description("Include archived subscriptions.")]
        public bool IncludeArchived { get; init; }

        [CommandOption("--deleted")]
        [Description("Include deleted subscriptions.")]
        public bool IncludeDeleted { get; init; }

        [CommandOption("--sort <FIELD>")]
        [Description("Sort by field (Name, Amount, Date, Category). Default is Name.")]
        public string? SortBy { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        
        var query = new SubscriptionQuery(
            Category: settings.Category,
            LifecycleStatus: settings.Status,
            IncludeArchived: settings.IncludeArchived,
            IncludeDeleted: settings.IncludeDeleted
        );

        var list = await runtime.Subscriptions.GetSubscriptionsAsync(query, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Query))
        {
            var q = settings.Query.Trim();
            list = list.Where(s =>
                s.ProviderName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.ServiceName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (s.PlanName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.AccountName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(settings.Tag))
        {
            var tagName = settings.Tag.Trim('#');
            list = list.Where(s => s.Tags.Any(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        list = (settings.SortBy?.ToLowerInvariant()) switch
        {
            "amount" => list.OrderByDescending(s => s.BillingAmount.Amount).ToList(),
            "date" => list.OrderBy(s => s.BillingSchedule.NextBillingOn ?? DateOnly.MaxValue).ToList(),
            "category" => list.OrderBy(s => s.Category).ThenBy(s => s.ProviderName).ToList(),
            _ => list.OrderBy(s => s.ProviderName).ThenBy(s => s.ServiceName).ToList()
        };

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(list.Select(s => new
            {
                s.Id,
                Category = s.Category.ToString(),
                s.ProviderName,
                s.ServiceName,
                s.PlanName,
                s.AccountName,
                BillingAmount = new { s.BillingAmount.Amount, s.BillingAmount.CurrencyCode },
                Status = s.LifecycleStatus.ToString(),
                NextBillingOn = s.BillingSchedule.NextBillingOn,
                Tags = s.Tags.Select(t => t.Name)
            }));
            return 0;
        }

        ConsoleFormatters.RenderSubscriptionsTable(list);
        return 0;
    }
}
