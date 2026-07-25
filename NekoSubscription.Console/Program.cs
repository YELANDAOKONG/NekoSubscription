using System;
using System.Threading.Tasks;
using Spectre.Console.Cli;
using NekoSubscription.Console.Commands;

namespace NekoSubscription.Console;

public static class Program
{
    public static ICommandApp CreateApp()
    {
        var app = new CommandApp<InteractiveCommand>();

        app.Configure(config =>
        {
            config.SetApplicationName("neko-sub");
            
            config.AddCommand<ListCommand>("list")
                .WithAlias("ls")
                .WithDescription("List subscriptions with optional filtering, sorting, and JSON formatting.");

            config.AddCommand<GetCommand>("get")
                .WithAlias("show")
                .WithDescription("Display detailed information for a subscription.");

            config.AddCommand<AddCommand>("add")
                .WithAlias("create")
                .WithDescription("Add a new subscription (supports interactive wizard or flags).");

            config.AddCommand<EditCommand>("edit")
                .WithAlias("update")
                .WithDescription("Edit an existing subscription.");

            config.AddCommand<DeleteCommand>("delete")
                .WithAlias("rm")
                .WithDescription("Soft delete a subscription.");

            config.AddCommand<ArchiveCommand>("archive")
                .WithDescription("Archive a subscription.");

            config.AddCommand<RestoreCommand>("restore")
                .WithDescription("Restore an archived or deleted subscription.");

            config.AddBranch("tag", tag =>
            {
                tag.SetDescription("Manage subscription tags.");
                tag.AddCommand<TagListCommand>("list").WithAlias("ls");
                tag.AddCommand<TagAddCommand>("add");
                tag.AddCommand<TagRenameCommand>("rename");
            });

            config.AddBranch("profile", profile =>
            {
                profile.SetDescription("Manage payment profiles.");
                profile.AddCommand<ProfileListCommand>("list").WithAlias("ls");
                profile.AddCommand<ProfileAddCommand>("add");
                profile.AddCommand<ProfileArchiveCommand>("archive");
                profile.AddCommand<ProfileRestoreCommand>("restore");
            });

            config.AddCommand<DashboardCommand>("dashboard")
                .WithAlias("overview")
                .WithDescription("Display overview dashboard and upcoming renewals.");

            config.AddCommand<CashFlowCommand>("cashflow")
                .WithAlias("forecast")
                .WithDescription("Display cash flow projection for a date range.");

            config.AddCommand<ExportCommand>("export")
                .WithDescription("Export subscriptions to a CSV file.");

            config.AddCommand<ImportCommand>("import")
                .WithDescription("Import subscriptions from a CSV file.");

            config.AddCommand<BackupCommand>("backup")
                .WithDescription("Create a database backup archive.");

            config.AddCommand<ClearCommand>("clear")
                .WithDescription("Clear all subscription data.");

            config.AddBranch("settings", settings =>
            {
                settings.SetDescription("View or modify application settings.");
                settings.AddCommand<SettingsGetCommand>("get");
                settings.AddCommand<SettingsSetCommand>("set");
            });

            config.AddCommand<InteractiveCommand>("interactive")
                .WithAlias("ui")
                .WithDescription("Launch interactive TUI menu mode.");
        });

        return app;
    }

    public static async Task<int> Main(string[] args)
    {
        var app = CreateApp();
        return await app.RunAsync(args);
    }
}