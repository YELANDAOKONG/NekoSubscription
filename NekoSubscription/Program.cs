using System;

using Avalonia;
using Avalonia.Media;

namespace NekoSubscription;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var runtime = new ApplicationRuntime();
        App.ConfigureRuntime(runtime);
        runtime.Start();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            runtime.CrashReports.TryWriteReport(exception, "Application entry point", true);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .With(new FontManagerOptions
            {
                // Bundled Noto fonts cover CJK + Latin glyphs; Inter is the final
                // fallback. Order: SC -> TC -> HK -> JP -> Noto Sans -> Inter.
                // NotoSansMono is bundled but intentionally not in the default chain
                // (monospaced); reference it explicitly where column alignment is needed.
                // NotoSans-Italic is listed so FontStyle=Italic requests resolve to the
                // bundled italic face instead of synthesizing oblique from the upright.
                DefaultFamilyName = "avares://NekoSubscription/Assets/Fonts/NotoSansSC-VariableFont_wght.ttf#Noto Sans SC"
                    + ", avares://NekoSubscription/Assets/Fonts/NotoSansTC-VariableFont_wght.ttf#Noto Sans TC"
                    + ", avares://NekoSubscription/Assets/Fonts/NotoSansHK-VariableFont_wght.ttf#Noto Sans HK"
                    + ", avares://NekoSubscription/Assets/Fonts/NotoSansJP-VariableFont_wght.ttf#Noto Sans JP"
                    + ", avares://NekoSubscription/Assets/Fonts/NotoSans-VariableFont_wdth,wght.ttf#Noto Sans"
                    + ", avares://Avalonia.Fonts.Inter/Assets/Inter-Regular.ttf#Inter",
            });
}
