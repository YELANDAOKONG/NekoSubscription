using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using NekoSubscription.Core.Subscriptions;
using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public partial class SubscriptionsViewModel : ViewModelBase
{
    private readonly List<SubscriptionListItemViewModel> _allSubscriptions = [];
    private readonly ILogger _logger;
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsViewModel(ISubscriptionService subscriptionService, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(subscriptionService);
        ArgumentNullException.ThrowIfNull(logger);

        _subscriptionService = subscriptionService;
        _logger = logger;
        RefreshLocalization();
    }

    public event Action<IReadOnlyList<Subscription>>? SnapshotChanged;

    public event Action<string>? StatusChanged;

    public ObservableCollection<SubscriptionListItemViewModel> Subscriptions { get; } = [];

    public ObservableCollection<SubscriptionCategoryFilter> CategoryFilters { get; } = [];

    public bool HasResults => Subscriptions.Count > 0;

    public bool HasNoResults => !HasResults;

    public bool HasSelectedSubscription => SelectedSubscription is not null;

    public string ResultSummary => Subscriptions.Count == 1
        ? AppResources.Get("Subscriptions_CountOne")
        : AppResources.Format("Subscriptions_CountMany", Subscriptions.Count);

    public string ArchiveActionLabel => SelectedSubscription?.IsArchived == true
        ? AppResources.Get("Subscriptions_Restore")
        : AppResources.Get("Subscriptions_Archive");

    [ObservableProperty]
    public partial bool IncludeArchived { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SubscriptionCategoryFilter SelectedCategoryFilter { get; set; } = null!;

    public void RefreshLocalization()
    {
        var selectedCategory = SelectedCategoryFilter?.Category;
        CategoryFilters.Clear();
        CategoryFilters.Add(new SubscriptionCategoryFilter(AppResources.Get("Filter_AllCategories"), null));
        CategoryFilters.Add(new SubscriptionCategoryFilter(
            AppResources.Get("Category_OrdinaryPlural"),
            SubscriptionCategory.Ordinary));
        CategoryFilters.Add(new SubscriptionCategoryFilter(
            AppResources.Get("Category_PhoneNumberPlural"),
            SubscriptionCategory.PhoneNumber));
        CategoryFilters.Add(new SubscriptionCategoryFilter(
            AppResources.Get("Category_DomainPlural"),
            SubscriptionCategory.Domain));
        CategoryFilters.Add(new SubscriptionCategoryFilter(
            AppResources.Get("Category_CloudServicePlural"),
            SubscriptionCategory.CloudService));
        CategoryFilters.Add(new SubscriptionCategoryFilter(
            AppResources.Get("Category_Custom"),
            SubscriptionCategory.Custom));
        SelectedCategoryFilter = CategoryFilters.First(filter => filter.Category == selectedCategory);
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(ArchiveActionLabel));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSubscription))]
    [NotifyPropertyChangedFor(nameof(ArchiveActionLabel))]
    public partial SubscriptionListItemViewModel? SelectedSubscription { get; set; }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusChanged?.Invoke(AppResources.Get("Status_RefreshingSubscriptions"));

        try
        {
            var subscriptions = await _subscriptionService.GetSubscriptionsAsync(
                new SubscriptionQuery(IncludeArchived: true));
            _allSubscriptions.Clear();
            _allSubscriptions.AddRange(
                subscriptions.Select(SubscriptionListItemViewModel.FromSubscription));
            ApplyFilters();
            SnapshotChanged?.Invoke(subscriptions);
            StatusChanged?.Invoke(AppResources.Format("Status_SubscriptionsLoaded", subscriptions.Count));
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to load subscriptions for the workspace.");
            StatusChanged?.Invoke(AppResources.Get("Status_LoadSubscriptionsFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

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
            StatusChanged?.Invoke(changed
                ? AppResources.Get("Status_ArchiveUpdated")
                : AppResources.Get("Status_SubscriptionMissing"));
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to update the subscription archive state.");
            StatusChanged?.Invoke(AppResources.Get("Status_ArchiveUpdateFailed"));
        }
        finally
        {
            IsBusy = false;
        }

        await RefreshAsync();
    }

    partial void OnIncludeArchivedChanged(bool value) => ApplyFilters();

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedCategoryFilterChanged(SubscriptionCategoryFilter value) => ApplyFilters();

    private void ApplyFilters()
    {
        var selectedId = SelectedSubscription?.Id;
        var normalizedSearchText = SearchText.Trim();
        var category = SelectedCategoryFilter?.Category;
        var filteredSubscriptions = _allSubscriptions.Where(subscription =>
            (IncludeArchived || !subscription.IsArchived) &&
            (category is null || subscription.Category == category) &&
            (normalizedSearchText.Length == 0 || subscription.Matches(normalizedSearchText)));

        Subscriptions.Clear();
        foreach (var subscription in filteredSubscriptions)
        {
            Subscriptions.Add(subscription);
        }

        SelectedSubscription = selectedId is { } id
            ? Subscriptions.FirstOrDefault(subscription => subscription.Id == id)
            : null;
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(ResultSummary));
    }
}
