using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;

namespace NekoSubscription.Console.Commands;

public sealed class ExportCommand : AsyncCommand<ExportCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("-o|--output <FILE>")]
        [Description("Output CSV file path.")]
        public string? OutputFile { get; init; }

        [CommandOption("--mask")]
        [Description("Mask sensitive account identifiers in exported CSV.")]
        public bool Mask { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);

        var outputPath = settings.OutputFile;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = AnsiConsole.Ask<string>("Enter [green]Output CSV File Path[/]:", "subscriptions_export.csv");
        }

        var fullPath = Path.GetFullPath(outputPath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = File.Create(fullPath);
        var result = await runtime.DataManagement.ExportSubscriptionCsvAsync(stream, settings.Mask, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Successfully exported {result.ExportedSubscriptionCount} subscription(s) to:[/] [bold]{fullPath}[/]");
        return 0;
    }
}

public sealed class ImportCommand : AsyncCommand<ImportCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("-i|--input <FILE>")]
        [Description("Input CSV file path.")]
        public string? InputFile { get; init; }

        [CommandOption("-y|--yes")]
        [Description("Bypass preview confirmation prompt.")]
        public bool Yes { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);

        var inputPath = settings.InputFile;
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            inputPath = AnsiConsole.Ask<string>("Enter [green]Input CSV File Path[/]:");
        }

        var fullPath = Path.GetFullPath(inputPath);
        if (!File.Exists(fullPath))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] File '[bold]{fullPath}[/]' does not exist.");
            return 1;
        }

        await using (var previewStream = File.OpenRead(fullPath))
        {
            var preview = await runtime.DataManagement.PreviewSubscriptionCsvAsync(previewStream, cancellationToken);
            AnsiConsole.MarkupLine($"[bold blue]CSV Import Preview:[/] Total Rows: [bold]{preview.TotalRowCount}[/], Valid Rows: [bold green]{preview.ValidRowCount}[/], Issues: [bold red]{preview.Issues.Count}[/]");

            if (preview.Issues.Count > 0)
            {
                foreach (var issue in preview.Issues)
                {
                    AnsiConsole.MarkupLine($"  [yellow]Row {issue.RowNumber}:[/] [{issue.Severity}] {issue.Code}");
                }
            }

            if (!settings.Yes && !AnsiConsole.Confirm("Proceed with importing valid rows?"))
            {
                AnsiConsole.MarkupLine("[yellow]Import cancelled.[/]");
                return 0;
            }
        }

        await using (var importStream = File.OpenRead(fullPath))
        {
            var result = await runtime.DataManagement.ImportSubscriptionCsvAsync(importStream, cancellationToken);
            AnsiConsole.MarkupLine($"[green]Successfully imported {result.ImportedSubscriptionCount} subscription(s).[/]");
        }

        return 0;
    }
}

public sealed class BackupCommand : AsyncCommand<BackupCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("-o|--output <FILE>")]
        [Description("Output backup zip file path.")]
        public string? OutputFile { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);

        var outputPath = settings.OutputFile;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var defaultName = $"neko_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            outputPath = AnsiConsole.Ask<string>("Enter [green]Output Backup Path[/]:", defaultName);
        }

        var fullPath = Path.GetFullPath(outputPath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var stream = File.Create(fullPath);
        await runtime.DataManagement.CreateBackupAsync(stream, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Successfully created backup archive at:[/] [bold]{fullPath}[/]");
        return 0;
    }
}

public sealed class ClearCommand : AsyncCommand<ClearCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("-y|--yes")]
        [Description("Bypass confirmation prompt.")]
        public bool Yes { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);

        if (!settings.Yes)
        {
            AnsiConsole.MarkupLine("[bold red]WARNING:[/] This action will delete ALL subscription data!");
            if (!AnsiConsole.Confirm("Are you sure you want to clear all data?"))
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                return 0;
            }
        }

        var result = await runtime.DataManagement.ClearSubscriptionDataAsync(cancellationToken);
        AnsiConsole.MarkupLine($"[green]Successfully cleared {result.DeletedSubscriptionCount} subscription(s).[/]");
        return 0;
    }
}
