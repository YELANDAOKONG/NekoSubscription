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

            string[] subArgs = choice switch
            {
                var c when c.StartsWith("📊") => new[] { "dashboard" },
                var c when c.StartsWith("📋") => new[] { "list" },
                var c when c.StartsWith("➕") => new[] { "add" },
                var c when c.StartsWith("💰") => new[] { "cashflow" },
                var c when c.StartsWith("🏷️") => new[] { "tag", "list" },
                var c when c.StartsWith("💳") => new[] { "profile", "list" },
                var c when c.StartsWith("📤") => new[] { "export" },
                var c when c.StartsWith("📥") => new[] { "import" },
                var c when c.StartsWith("💾") => new[] { "backup" },
                var c when c.StartsWith("⚙️") => new[] { "settings", "get" },
                _ => Array.Empty<string>()
            };

            if (subArgs.Length > 0)
            {
                var fullArgs = new List<string>(subArgs);
                if (!string.IsNullOrWhiteSpace(settings.DataRoot))
                {
                    fullArgs.Add("-d");
                    fullArgs.Add(settings.DataRoot);
                }

                try
                {
                    var app = Program.CreateApp();
                    await app.RunAsync(fullArgs.ToArray());
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
}
