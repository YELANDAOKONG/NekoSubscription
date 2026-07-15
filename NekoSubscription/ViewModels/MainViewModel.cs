using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.Subscriptions;

namespace NekoSubscription.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const int ProjectionDayCount = 90;

    private readonly CashFlowProjector _cashFlowProjector;
    private readonly ILogger _logger;
    private readonly IApplicationSettingsService _settingsService;
    private readonly ISubscriptionService _subscriptionService;
    private ApplicationSettings _settings = new();
    private bool _isApplyingSettings;

    public MainViewModel(
        ISubscriptionService subscriptionService,
        IApplicationSettingsService settingsService,
        CashFlowProjector cashFlowProjector,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(subscriptionService);
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(cashFlowProjector);
        ArgumentNullException.ThrowIfNull(logger);

        _subscriptionService = subscriptionService;
        _settingsService = settingsService;
        _cashFlowProjector = cashFlowProjector;
        _logger = logger;
        VisualStyles = Enum.GetValues<ApplicationVisualStyle>();
    }

    public event EventHandler? AppearanceChanged;

    public ObservableCollection<SubscriptionListItemViewModel> Subscriptions { get; } = [];

    public ObservableCollection<CurrencyTotalViewModel> CurrencyTotals { get; } = [];

    public IReadOnlyList<ApplicationVisualStyle> VisualStyles { get; }

    public bool IsAcrylicSelected => SelectedVisualStyle == ApplicationVisualStyle.Acrylic;

    public string AcrylicOpacityLabel => $"{AcrylicOpacity:P0}";

    public bool HasSelectedSubscription => SelectedSubscription is not null;

    public string ArchiveActionLabel => SelectedSubscription?.IsArchived == true
        ? "Restore from archive"
        : "Archive";

    [ObservableProperty]
    public partial bool IncludeArchived { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Loading subscriptions...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSubscription))]
    [NotifyPropertyChangedFor(nameof(ArchiveActionLabel))]
    public partial SubscriptionListItemViewModel? SelectedSubscription { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAcrylicSelected))]
    public partial ApplicationVisualStyle SelectedVisualStyle { get; set; } = ApplicationVisualStyle.Standard;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AcrylicOpacityLabel))]
    public partial double AcrylicOpacity { get; set; } = ApplicationSettings.DefaultAcrylicOpacity;

    public async Task InitializeAsync(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _isApplyingSettings = true;
        SelectedVisualStyle = settings.VisualStyle;
        AcrylicOpacity = settings.AcrylicOpacity;
        _isApplyingSettings = false;
        RaiseAppearanceChanged();

        await RefreshCoreAsync();
    }

    [RelayCommand]
    private Task RefreshAsync() => RefreshCoreAsync();

    [RelayCommand]
    private async Task ToggleArchiveAsync()
    {
        if (SelectedSubscription is not { } selectedSubscription || IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var changed = selectedSubscription.IsArchived
                ? await _subscriptionService.RestoreSubscriptionFromArchiveAsync(selectedSubscription.Id)
                : await _subscriptionService.ArchiveSubscriptionAsync(selectedSubscription.Id);
            StatusMessage = changed ? "Subscription archive state updated." : "Subscription was not found.";
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to update the subscription archive state.");
            StatusMessage = "Unable to update the subscription archive state.";
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshCoreAsync();
    }

    [RelayCommand]
    private async Task SaveAppearanceAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            _settings.VisualStyle = SelectedVisualStyle;
            _settings.AcrylicOpacity = AcrylicOpacity;
            await _settingsService.SaveAsync(_settings);
            StatusMessage = "Appearance settings saved.";
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to save appearance settings.");
            StatusMessage = "Unable to save appearance settings.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedVisualStyleChanged(ApplicationVisualStyle value)
    {
        RaiseAppearanceChanged();
    }

    partial void OnAcrylicOpacityChanged(double value)
    {
        RaiseAppearanceChanged();
    }

    private async Task RefreshCoreAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Loading subscriptions...";

        try
        {
            var subscriptions = await _subscriptionService.GetSubscriptionsAsync(
                new SubscriptionQuery(IncludeArchived: IncludeArchived));
            ReplaceItems(
                Subscriptions,
                subscriptions.Select(SubscriptionListItemViewModel.FromSubscription));

            var projectionStartsOn = DateOnly.FromDateTime(DateTime.Today);
            var projectionEndsOn = projectionStartsOn.AddDays(ProjectionDayCount - 1);
            var visibleSubscriptions = subscriptions.Where(subscription => !subscription.IsArchived);
            var projection = _cashFlowProjector.Project(
                visibleSubscriptions,
                projectionStartsOn,
                projectionEndsOn);
            ReplaceItems(
                CurrencyTotals,
                projection.CurrencyTotals.Select(CurrencyTotalViewModel.FromTotal));

            StatusMessage = $"{Subscriptions.Count} subscriptions shown; {projection.Items.Count} payments projected for the next {ProjectionDayCount} days.";
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to load subscriptions and cash flow projection.");
            StatusMessage = "Unable to load subscriptions.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();

        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private void RaiseAppearanceChanged()
    {
        if (_isApplyingSettings)
        {
            return;
        }

        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }
}
