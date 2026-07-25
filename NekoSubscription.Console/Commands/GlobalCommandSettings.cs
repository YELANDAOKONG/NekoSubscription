using System.ComponentModel;
using Spectre.Console.Cli;

namespace NekoSubscription.Console.Commands;

public class GlobalCommandSettings : CommandSettings
{
    [CommandOption("-d|--data-root <PATH>")]
    [Description("Override the application data root directory path.")]
    public string? DataRoot { get; init; }

    [CommandOption("--json")]
    [Description("Output command results in JSON format.")]
    public bool Json { get; init; }
}
