using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

namespace NekoSubscription.Console.Commands;

public sealed class InteractiveCommand : AsyncCommand<GlobalCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GlobalCommandSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.Write(
            new FigletText("NekoSubscription")
                .LeftJustified()
                .Color(Color.Purple));

        AnsiConsole.MarkupLine("[dim]Welcome to NekoSubscription Console CLI![/]\n");

        while (!cancellationToken.IsCancellationRequested)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose an [bold green]action[/]:")
                    .PageSize(12)
                    .AddChoices(new[]
                    {
                        "📊 Dashboard & Overview",
                        "📋 List Subscriptions",
                        "➕ Add Subscription",
                        "💰 Cash Flow Forecast",
                        "🏷️  Manage Tags",
                        "💳 Manage Payment Profiles",
                        "📤 Export Subscriptions (CSV)",
                        "📥 Import Subscriptions (CSV)",
                        "💾 Backup Database",
                        "⚙️  Application Settings",
                        "🚪 Exit"
                    }));

            if (choice.StartsWith("🚪"))
            {
                AnsiConsole.MarkupLine("[grey]Goodbye![/]");
                break;
            }

            List<string> subArgs = choice switch
            {
                var c when c.StartsWith("📊") => PromptDashboardArgs(),
                var c when c.StartsWith("📋") => new List<string> { "list" },
                var c when c.StartsWith("➕") => new List<string> { "add" },
                var c when c.StartsWith("💰") => new List<string> { "cashflow" },
                var c when c.StartsWith("🏷️") => new List<string> { "tag", "list" },
                var c when c.StartsWith("💳") => new List<string> { "profile", "list" },
                var c when c.StartsWith("📤") => new List<string> { "export" },
                var c when c.StartsWith("📥") => new List<string> { "import" },
                var c when c.StartsWith("💾") => new List<string> { "backup" },
                var c when c.StartsWith("⚙️") => new List<string> { "settings", "get" },
                _ => new List<string>()
            };

            if (subArgs.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(settings.DataRoot))
                {
                    subArgs.Add("-d");
                    subArgs.Add(settings.DataRoot);
                }

                try
                {
                    var app = Program.CreateApp();
                    await app.RunAsync(subArgs.ToArray());
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }

            AnsiConsole.WriteLine();
        }

        return 0;
    }

    private static List<string> PromptDashboardArgs()
    {
        var period = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select [bold green]Forecast Period[/]:")
                .AddChoices("7 Days (Default)", "3 Days", "14 Days", "30 Days", "90 Days"));

        var daysStr = period switch
        {
            "3 Days" => "3",
            "14 Days" => "14",
            "30 Days" => "30",
            "90 Days" => "90",
            _ => "7"
        };

        return new List<string> { "dashboard", "--days", daysStr };
    }
}
