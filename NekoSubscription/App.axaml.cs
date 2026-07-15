using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

using Serilog;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Core.Configuration;
using NekoSubscription.ViewModels;
using NekoSubscription.Views;

namespace NekoSubscription;

public partial class App : Application
{
    private static ApplicationRuntime? _runtime;

    internal static void ConfigureRuntime(ApplicationRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _runtime = runtime;
    }

    public override void Initialize()
    {
        _runtime?.StartAvaloniaDiagnostics();
        RequestedThemeVariant = ThemeVariant.Default;
        DataTemplates.Add(new ViewLocator());
        Styles.Add(new FluentTheme());
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        if (_runtime is null)
        {
            throw new InvalidOperationException("The application runtime has not been configured.");
        }

        var viewModel = new MainViewModel(
            _runtime.Subscriptions,
            _runtime.Settings,
            new CashFlowProjector(),
            _runtime.Logger);
        desktop.MainWindow = new MainWindow(viewModel);
        base.OnFrameworkInitializationCompleted();

        var settings = new ApplicationSettings();
        try
        {
            settings = await _runtime.Settings.GetAsync();
            ApplySettings(settings);
            _runtime.Logging.MinimumLevel = settings.MinimumLogLevel;
            _runtime.Logger.Information("Application settings loaded.");
        }
        catch (Exception exception)
        {
            _runtime.Logger.Error(
                exception,
                "Failed to load application settings. Defaults remain active.");
        }

        try
        {
            await _runtime.Subscriptions.InitializeAsync();
            _runtime.Logger.Information("Subscription database initialized.");
        }
        catch (Exception exception)
        {
            _runtime.Logger.Error(exception, "Failed to initialize the subscription database.");
        }

        await viewModel.InitializeAsync(settings);
    }

    private void ApplySettings(ApplicationSettings settings)
    {
        RequestedThemeVariant = settings.Theme switch
        {
            ApplicationTheme.System => ThemeVariant.Default,
            ApplicationTheme.Light => ThemeVariant.Light,
            ApplicationTheme.Dark => ThemeVariant.Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Theme, "The application theme is invalid.")
        };

        var culture = settings.CultureName is null
            ? null
            : CultureInfo.GetCultureInfo(settings.CultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
