using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using NekoSubscription.Console.Infrastructure;
using NekoSubscription.Core.Configuration;

namespace NekoSubscription.Console.Commands;

public sealed class SettingsGetCommand : AsyncCommand<GlobalCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GlobalCommandSettings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var appSettings = await runtime.Settings.GetAsync(cancellationToken);

        if (settings.Json)
        {
            ConsoleFormatters.PrintJson(appSettings);
            return 0;
        }

        ConsoleFormatters.RenderSettings(appSettings);
        return 0;
    }
}

public sealed class SettingsSetCommand : AsyncCommand<SettingsSetCommand.Settings>
{
    public sealed class Settings : GlobalCommandSettings
    {
        [CommandOption("--theme <THEME>")]
        [Description("Application theme: System, Light, Dark.")]
        public ApplicationTheme? Theme { get; init; }

        [CommandOption("--log-level <LEVEL>")]
        [Description("Log level: Verbose, Debug, Information, Warning, Error, Fatal.")]
        public ApplicationLogLevel? LogLevel { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        await using var runtime = await ConsoleRuntime.CreateAsync(settings.DataRoot, cancellationToken);
        var current = await runtime.Settings.GetAsync(cancellationToken);

        if (settings.Theme.HasValue)
        {
            current.Theme = settings.Theme.Value;
        }

        if (settings.LogLevel.HasValue)
        {
            current.MinimumLogLevel = settings.LogLevel.Value;
        }

        await runtime.Settings.SaveAsync(current, cancellationToken);

        AnsiConsole.MarkupLine("[green]Successfully updated application settings.[/]");
        ConsoleFormatters.RenderSettings(current);
        return 0;
    }
}
