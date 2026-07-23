using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using NekoSubscription.Core.CashFlow;
using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.Subscriptions;
using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public partial class MainViewModel : ViewModelBase
{
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

        Dashboard = new DashboardViewModel(cashFlowProjector);
        Calendar = new CalendarViewModel(cashFlowProjector);
        Subscriptions = new SubscriptionsViewModel(subscriptionService, logger);
        Settings = new SettingsViewModel(settingsService, logger);
        CurrentPage = Dashboard;

        Subscriptions.SnapshotChanged += OnSnapshotChanged;
        Subscriptions.StatusChanged += SetStatus;
        Calendar.SubscriptionRequested += OnSubscriptionRequested;
        Dashboard.SubscriptionRequested += OnSubscriptionRequested;
        Settings.StatusChanged += SetStatus;
        Settings.AppearanceChanged += OnAppearanceChanged;
        Settings.CultureChanged += OnCultureChanged;
        Subscriptions.PropertyChanged += OnChildPropertyChanged;
        Settings.PropertyChanged += OnChildPropertyChanged;

        RefreshPageMetadata();
    }

    public event EventHandler? AppearanceChanged;

    public event EventHandler? LanguageChanged;

    public DashboardViewModel Dashboard { get; }

    public CalendarViewModel Calendar { get; }

    public SubscriptionsViewModel Subscriptions { get; }

    public SettingsViewModel Settings { get; }

    public bool IsDashboardSelected => CurrentPage == Dashboard;

    public bool IsCalendarSelected => CurrentPage == Calendar;

    public bool IsSubscriptionsSelected => CurrentPage == Subscriptions;

    public bool IsSettingsSelected => CurrentPage == Settings;

    public bool IsBusy => Subscriptions.IsBusy || Settings.IsBusy;

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; private set; }

    [ObservableProperty]
    public partial string PageSubtitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string PageTitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; private set; } = AppResources.Get("Status_Starting");

    public async Task InitializeAsync(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Settings.Initialize(settings);
        Subscriptions.RefreshLocalization();
        RefreshPageMetadata();
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        await Subscriptions.RefreshAsync();
    }

    [RelayCommand]
    private void ShowDashboard() => Navigate(Dashboard);

    [RelayCommand]
    private void ShowCalendar() => Navigate(Calendar);

    [RelayCommand]
    private void ShowSubscriptions() => Navigate(Subscriptions);

    [RelayCommand]
    private void ShowSettings() => Navigate(Settings);

    private void Navigate(ViewModelBase page)
    {
        CurrentPage = page;
        RefreshPageMetadata();
        OnPropertyChanged(nameof(IsDashboardSelected));
        OnPropertyChanged(nameof(IsCalendarSelected));
        OnPropertyChanged(nameof(IsSubscriptionsSelected));
        OnPropertyChanged(nameof(IsSettingsSelected));
    }

    private void RefreshPageMetadata()
    {
        if (CurrentPage == Dashboard)
        {
            PageTitle = AppResources.Get("Nav_Overview");
            PageSubtitle = AppResources.Get("Page_OverviewSubtitle");
            return;
        }

        if (CurrentPage == Subscriptions)
        {
            PageTitle = AppResources.Get("Nav_Subscriptions");
            PageSubtitle = AppResources.Get("Page_SubscriptionsSubtitle");
            return;
        }

        if (CurrentPage == Calendar)
        {
            PageTitle = AppResources.Get("Nav_Calendar");
            PageSubtitle = AppResources.Get("Page_CalendarSubtitle");
            return;
        }

        PageTitle = AppResources.Get("Nav_Settings");
        PageSubtitle = AppResources.Get("Page_SettingsSubtitle");
    }

    private void OnAppearanceChanged(object? sender, EventArgs e) =>
        AppearanceChanged?.Invoke(this, EventArgs.Empty);

    private async void OnCultureChanged(object? sender, EventArgs e)
    {
        Subscriptions.RefreshLocalization();
        Dashboard.RefreshLocalization();
        Calendar.RefreshLocalization();
        RefreshPageMetadata();
        StatusMessage = AppResources.Get("Status_Starting");
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        await Subscriptions.RefreshAsync();
    }

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SubscriptionsViewModel.IsBusy) ||
            e.PropertyName == nameof(SettingsViewModel.IsBusy))
        {
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    private void SetStatus(string message) => StatusMessage = message;

    private void OnSubscriptionRequested(Guid subscriptionId)
    {
        Subscriptions.SelectSubscription(subscriptionId);
        Navigate(Subscriptions);
    }

    private void OnSnapshotChanged(IReadOnlyList<Subscription> subscriptions)
    {
        Dashboard.Update(subscriptions);
        Calendar.Update(subscriptions);
    }
}
