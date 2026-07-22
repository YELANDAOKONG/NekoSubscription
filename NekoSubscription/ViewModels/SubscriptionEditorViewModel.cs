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

public partial class SubscriptionEditorViewModel : ViewModelBase
{
    private readonly Action _cancel;
    private readonly Func<Task> _saved;
    private readonly ILogger _logger;
    private readonly ISubscriptionService _subscriptionService;
    private readonly Guid? _subscriptionId;

    private SubscriptionEditorViewModel(
        ISubscriptionService subscriptionService,
        ILogger logger,
        Func<Task> saved,
        Action cancel,
        Subscription? subscription)
    {
        ArgumentNullException.ThrowIfNull(subscriptionService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(saved);
        ArgumentNullException.ThrowIfNull(cancel);

        _subscriptionService = subscriptionService;
        _logger = logger;
        _saved = saved;
        _cancel = cancel;
        _subscriptionId = subscription?.Id;

        RefreshLocalization();
        Load(subscription);
    }

    public ObservableCollection<SelectionOption<BillingCadence>> BillingCadences { get; } = [];

    public ObservableCollection<SelectionOption<BillingIntervalUnit>> BillingIntervalUnits { get; } = [];

    public ObservableCollection<SelectionOption<CloudBillingMode>> CloudBillingModes { get; } = [];

    public ObservableCollection<SelectionOption<SubscriptionConfirmationStatus>> ConfirmationStatuses { get; } = [];

    public ObservableCollection<SelectionOption<CurrencyKind>> CurrencyKinds { get; } = [];

    public ObservableCollection<SelectionOption<SubscriptionImportance>> ImportanceOptions { get; } = [];

    public ObservableCollection<SelectionOption<SubscriptionLifecycleStatus>> LifecycleStatuses { get; } = [];

    public ObservableCollection<SelectionOption<PhoneNumberType>> PhoneNumberTypes { get; } = [];

    public ObservableCollection<SelectionOption<SubscriptionCategory>> SubscriptionCategories { get; } = [];

    public bool CanChangeCategory => !IsEditing;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsCloudService => SelectedCategoryOption.Value == SubscriptionCategory.CloudService;

    public bool IsDomain => SelectedCategoryOption.Value == SubscriptionCategory.Domain;

    public bool IsEditing => _subscriptionId is not null;

    public bool IsPhoneNumber => SelectedCategoryOption.Value == SubscriptionCategory.PhoneNumber;

    public bool IsRecurring => SelectedBillingCadenceOption.Value == BillingCadence.Recurring;

    public string Title => AppResources.Get(IsEditing
        ? "Editor_EditTitle"
        : "Editor_AddTitle");

    [ObservableProperty]
    public partial decimal Amount { get; set; }

    [ObservableProperty]
    public partial string AccountName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool AutomaticallyRenews { get; set; } = true;

    [ObservableProperty]
    public partial string CarrierName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrencyCode { get; set; } = "USD";

    [ObservableProperty]
    public partial DateTimeOffset? DomainExpiresOn { get; set; }

    [ObservableProperty]
    public partial string DomainName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? DomainRegisteredOn { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial decimal IntervalCount { get; set; } = 1;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial bool IsPrepaid { get; set; }

    [ObservableProperty]
    public partial string ManagementUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? NextBillingOn { get; set; }

    [ObservableProperty]
    public partial string PhoneNumber { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PlanName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProjectIdentifier { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ProviderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RegionName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SelectionOption<BillingCadence> SelectedBillingCadenceOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<BillingIntervalUnit> SelectedBillingIntervalUnitOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<CloudBillingMode> SelectedCloudBillingModeOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<SubscriptionConfirmationStatus> SelectedConfirmationStatusOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<CurrencyKind> SelectedCurrencyKindOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<SubscriptionImportance> SelectedImportanceOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<SubscriptionLifecycleStatus> SelectedLifecycleStatusOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<PhoneNumberType> SelectedPhoneNumberTypeOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<SubscriptionCategory> SelectedCategoryOption { get; set; } = null!;

    [ObservableProperty]
    public partial string ServiceName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? StartsOn { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? EndsOn { get; set; }

    [ObservableProperty]
    public partial string TenantIdentifier { get; set; } = string.Empty;

    public static SubscriptionEditorViewModel CreateNew(
        ISubscriptionService subscriptionService,
        ILogger logger,
        Func<Task> saved,
        Action cancel) =>
        new(subscriptionService, logger, saved, cancel, null);

    public static SubscriptionEditorViewModel CreateForEdit(
        ISubscriptionService subscriptionService,
        ILogger logger,
        Func<Task> saved,
        Action cancel,
        Subscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        return new SubscriptionEditorViewModel(subscriptionService, logger, saved, cancel, subscription);
    }

    public void RefreshLocalization()
    {
        var selectedCategory = SelectedCategoryOption?.Value ?? SubscriptionCategory.Ordinary;
        var selectedCadence = SelectedBillingCadenceOption?.Value ?? BillingCadence.Recurring;
        var selectedInterval = SelectedBillingIntervalUnitOption?.Value ?? BillingIntervalUnit.Month;
        var selectedCloudMode = SelectedCloudBillingModeOption?.Value ?? CloudBillingMode.Fixed;
        var selectedConfirmation = SelectedConfirmationStatusOption?.Value ?? SubscriptionConfirmationStatus.Unknown;
        var selectedCurrencyKind = SelectedCurrencyKindOption?.Value ?? CurrencyKind.Iso4217;
        var selectedImportance = SelectedImportanceOption?.Value ?? SubscriptionImportance.Normal;
        var selectedLifecycle = SelectedLifecycleStatusOption?.Value ?? SubscriptionLifecycleStatus.Unknown;
        var selectedPhoneType = SelectedPhoneNumberTypeOption?.Value ?? PhoneNumberType.Mobile;

        ReplaceOptions(SubscriptionCategories, CreateCategoryOptions());
        ReplaceOptions(BillingCadences, CreateBillingCadenceOptions());
        ReplaceOptions(BillingIntervalUnits, CreateIntervalOptions());
        ReplaceOptions(CloudBillingModes, CreateCloudBillingModeOptions());
        ReplaceOptions(ConfirmationStatuses, CreateConfirmationOptions());
        ReplaceOptions(CurrencyKinds, CreateCurrencyKindOptions());
        ReplaceOptions(ImportanceOptions, CreateImportanceOptions());
        ReplaceOptions(LifecycleStatuses, CreateLifecycleOptions());
        ReplaceOptions(PhoneNumberTypes, CreatePhoneNumberTypeOptions());

        SelectedCategoryOption = FindOption(SubscriptionCategories, selectedCategory);
        SelectedBillingCadenceOption = FindOption(BillingCadences, selectedCadence);
        SelectedBillingIntervalUnitOption = FindOption(BillingIntervalUnits, selectedInterval);
        SelectedCloudBillingModeOption = FindOption(CloudBillingModes, selectedCloudMode);
        SelectedConfirmationStatusOption = FindOption(ConfirmationStatuses, selectedConfirmation);
        SelectedCurrencyKindOption = FindOption(CurrencyKinds, selectedCurrencyKind);
        SelectedImportanceOption = FindOption(ImportanceOptions, selectedImportance);
        SelectedLifecycleStatusOption = FindOption(LifecycleStatuses, selectedLifecycle);
        SelectedPhoneNumberTypeOption = FindOption(PhoneNumberTypes, selectedPhoneType);
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand]
    private void Cancel()
    {
        if (!IsBusy)
        {
            _cancel();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var billingAmount = new Money(Amount, CurrencyCode, SelectedCurrencyKindOption.Value);
            var billingSchedule = CreateBillingSchedule();

            if (_subscriptionId is { } subscriptionId)
            {
                var updated = await _subscriptionService.UpdateSubscriptionAsync(
                    subscriptionId,
                    (subscription, changedAtUtc) => ApplyUpdate(
                        subscription,
                        billingAmount,
                        billingSchedule,
                        changedAtUtc));
                if (!updated)
                {
                    ErrorMessage = AppResources.Get("Editor_SubscriptionMissing");
                    return;
                }
            }
            else
            {
                var subscription = CreateSubscription(billingAmount, billingSchedule);
                ApplyCommonState(subscription, DateTimeOffset.UtcNow);
                await _subscriptionService.AddSubscriptionAsync(subscription);
            }

            await _saved();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _logger.Warning(exception, "Subscription editor validation failed.");
            ErrorMessage = AppResources.Get("Editor_ValidationFailed");
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to save a subscription from the editor.");
            ErrorMessage = AppResources.Get("Editor_SaveFailed");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedBillingCadenceOptionChanged(SelectionOption<BillingCadence> value)
    {
        OnPropertyChanged(nameof(IsRecurring));
    }

    partial void OnSelectedCategoryOptionChanged(SelectionOption<SubscriptionCategory> value)
    {
        OnPropertyChanged(nameof(IsCloudService));
        OnPropertyChanged(nameof(IsDomain));
        OnPropertyChanged(nameof(IsPhoneNumber));
    }

    private void Load(Subscription? subscription)
    {
        if (subscription is null)
        {
            return;
        }

        SelectedCategoryOption = FindOption(SubscriptionCategories, subscription.Category);
        ProviderName = subscription.ProviderName;
        ServiceName = subscription.ServiceName;
        PlanName = subscription.PlanName ?? string.Empty;
        AccountName = subscription.AccountName ?? string.Empty;
        Amount = subscription.BillingAmount.Amount;
        CurrencyCode = subscription.BillingAmount.CurrencyCode;
        SelectedCurrencyKindOption = FindOption(CurrencyKinds, subscription.BillingAmount.CurrencyKind);
        SelectedBillingCadenceOption = FindOption(BillingCadences, subscription.BillingSchedule.Cadence);
        SelectedBillingIntervalUnitOption = FindOption(
            BillingIntervalUnits,
            subscription.BillingSchedule.IntervalUnit ?? BillingIntervalUnit.Month);
        IntervalCount = subscription.BillingSchedule.IntervalCount ?? 1;
        StartsOn = ToDateTimeOffset(subscription.BillingSchedule.StartsOn);
        NextBillingOn = ToDateTimeOffset(subscription.BillingSchedule.NextBillingOn);
        EndsOn = ToDateTimeOffset(subscription.BillingSchedule.EndsOn);
        AutomaticallyRenews = subscription.BillingSchedule.AutomaticallyRenews;
        SelectedConfirmationStatusOption = FindOption(ConfirmationStatuses, subscription.ConfirmationStatus);
        SelectedLifecycleStatusOption = FindOption(LifecycleStatuses, subscription.LifecycleStatus);
        SelectedImportanceOption = FindOption(ImportanceOptions, subscription.Importance);
        Notes = subscription.Notes ?? string.Empty;
        ManagementUrl = subscription.ManagementUrl ?? string.Empty;

        switch (subscription)
        {
            case PhoneNumberSubscription phoneNumberSubscription:
                PhoneNumber = phoneNumberSubscription.PhoneNumber;
                SelectedPhoneNumberTypeOption = FindOption(
                    PhoneNumberTypes,
                    phoneNumberSubscription.PhoneNumberType);
                CarrierName = phoneNumberSubscription.CarrierName;
                RegionName = phoneNumberSubscription.RegionName ?? string.Empty;
                IsPrepaid = phoneNumberSubscription.IsPrepaid;
                break;
            case DomainSubscription domainSubscription:
                DomainName = domainSubscription.DomainName;
                DomainRegisteredOn = ToDateTimeOffset(domainSubscription.RegisteredOn);
                DomainExpiresOn = ToDateTimeOffset(domainSubscription.ExpiresOn);
                break;
            case CloudServiceSubscription cloudServiceSubscription:
                SelectedCloudBillingModeOption = FindOption(
                    CloudBillingModes,
                    cloudServiceSubscription.BillingMode);
                TenantIdentifier = cloudServiceSubscription.TenantIdentifier ?? string.Empty;
                ProjectIdentifier = cloudServiceSubscription.ProjectIdentifier ?? string.Empty;
                break;
            case OrdinarySubscription:
            case CustomSubscription:
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(subscription),
                    subscription.Category,
                    "The subscription category is unsupported.");
        }

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(CanChangeCategory));
    }

    private BillingSchedule CreateBillingSchedule()
    {
        var cadence = SelectedBillingCadenceOption.Value;
        return new BillingSchedule(
            cadence,
            cadence == BillingCadence.Recurring ? SelectedBillingIntervalUnitOption.Value : null,
            cadence == BillingCadence.Recurring ? ConvertIntervalCount() : null,
            ToDateOnly(StartsOn),
            ToDateOnly(NextBillingOn),
            ToDateOnly(EndsOn),
            cadence == BillingCadence.Recurring && AutomaticallyRenews);
    }

    private Subscription CreateSubscription(Money billingAmount, BillingSchedule billingSchedule)
    {
        return SelectedCategoryOption.Value switch
        {
            SubscriptionCategory.Ordinary => new OrdinarySubscription(
                ProviderName,
                ServiceName,
                PlanName,
                AccountName,
                billingAmount,
                billingSchedule),
            SubscriptionCategory.PhoneNumber => new PhoneNumberSubscription(
                ProviderName,
                ServiceName,
                PlanName,
                AccountName,
                billingAmount,
                billingSchedule,
                PhoneNumber,
                SelectedPhoneNumberTypeOption.Value,
                CarrierName,
                RegionName,
                IsPrepaid),
            SubscriptionCategory.Domain => new DomainSubscription(
                ProviderName,
                ServiceName,
                PlanName,
                AccountName,
                billingAmount,
                billingSchedule,
                DomainName,
                ToDateOnly(DomainRegisteredOn),
                ToDateOnly(DomainExpiresOn)),
            SubscriptionCategory.CloudService => new CloudServiceSubscription(
                ProviderName,
                ServiceName,
                PlanName,
                AccountName,
                billingAmount,
                billingSchedule,
                SelectedCloudBillingModeOption.Value,
                TenantIdentifier,
                ProjectIdentifier),
            SubscriptionCategory.Custom => new CustomSubscription(
                ProviderName,
                ServiceName,
                PlanName,
                AccountName,
                billingAmount,
                billingSchedule),
            _ => throw new ArgumentOutOfRangeException(
                nameof(SelectedCategoryOption),
                SelectedCategoryOption.Value,
                "The subscription category is unsupported.")
        };
    }

    private void ApplyUpdate(
        Subscription subscription,
        Money billingAmount,
        BillingSchedule billingSchedule,
        DateTimeOffset changedAtUtc)
    {
        subscription.UpdateIdentity(ProviderName, ServiceName, PlanName, AccountName, changedAtUtc);
        subscription.UpdateBilling(billingAmount, billingSchedule, changedAtUtc);
        ApplyCommonState(subscription, changedAtUtc);

        switch (subscription)
        {
            case PhoneNumberSubscription phoneNumberSubscription:
                phoneNumberSubscription.SetPhoneNumberDetails(
                    PhoneNumber,
                    SelectedPhoneNumberTypeOption.Value,
                    CarrierName,
                    RegionName,
                    IsPrepaid,
                    changedAtUtc);
                break;
            case DomainSubscription domainSubscription:
                domainSubscription.SetDomainDetails(
                    DomainName,
                    ToDateOnly(DomainRegisteredOn),
                    ToDateOnly(DomainExpiresOn),
                    changedAtUtc);
                break;
            case CloudServiceSubscription cloudServiceSubscription:
                cloudServiceSubscription.SetCloudDetails(
                    SelectedCloudBillingModeOption.Value,
                    TenantIdentifier,
                    ProjectIdentifier,
                    changedAtUtc);
                break;
            case OrdinarySubscription:
            case CustomSubscription:
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(subscription),
                    subscription.Category,
                    "The subscription category is unsupported.");
        }
    }

    private void ApplyCommonState(Subscription subscription, DateTimeOffset changedAtUtc)
    {
        subscription.SetStatuses(
            SelectedConfirmationStatusOption.Value,
            SelectedLifecycleStatusOption.Value,
            changedAtUtc);
        subscription.SetImportance(SelectedImportanceOption.Value, changedAtUtc);
        subscription.UpdateNotesAndManagementUrl(Notes, ManagementUrl, changedAtUtc);
    }

    private int ConvertIntervalCount()
    {
        if (IntervalCount is <= 0 or > int.MaxValue || decimal.Truncate(IntervalCount) != IntervalCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(IntervalCount),
                IntervalCount,
                "The billing interval must be a positive whole number.");
        }

        return decimal.ToInt32(IntervalCount);
    }

    private static DateOnly? ToDateOnly(DateTimeOffset? value) =>
        value is { } date ? DateOnly.FromDateTime(date.Date) : null;

    private static DateTimeOffset? ToDateTimeOffset(DateOnly? value) =>
        value is { } date
            ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null;

    private static SelectionOption<T> FindOption<T>(
        IEnumerable<SelectionOption<T>> options,
        T value) where T : struct, Enum =>
        options.Single(option => EqualityComparer<T>.Default.Equals(option.Value, value));

    private static void ReplaceOptions<T>(
        ObservableCollection<SelectionOption<T>> target,
        IEnumerable<SelectionOption<T>> options)
    {
        target.Clear();
        foreach (var option in options)
        {
            target.Add(option);
        }
    }

    private static SelectionOption<SubscriptionCategory>[] CreateCategoryOptions() =>
    [
        new(AppResources.Get("Category_Ordinary"), SubscriptionCategory.Ordinary),
        new(AppResources.Get("Category_PhoneNumber"), SubscriptionCategory.PhoneNumber),
        new(AppResources.Get("Category_Domain"), SubscriptionCategory.Domain),
        new(AppResources.Get("Category_CloudService"), SubscriptionCategory.CloudService),
        new(AppResources.Get("Category_Custom"), SubscriptionCategory.Custom)
    ];

    private static SelectionOption<BillingCadence>[] CreateBillingCadenceOptions() =>
    [
        new(AppResources.Get("Schedule_Recurring"), BillingCadence.Recurring),
        new(AppResources.Get("Schedule_OneTime"), BillingCadence.OneTime),
        new(AppResources.Get("Schedule_Manual"), BillingCadence.Manual)
    ];

    private static SelectionOption<BillingIntervalUnit>[] CreateIntervalOptions() =>
    [
        new(AppResources.Get("Interval_Day"), BillingIntervalUnit.Day),
        new(AppResources.Get("Interval_Week"), BillingIntervalUnit.Week),
        new(AppResources.Get("Interval_Month"), BillingIntervalUnit.Month),
        new(AppResources.Get("Interval_Year"), BillingIntervalUnit.Year)
    ];

    private static SelectionOption<CurrencyKind>[] CreateCurrencyKindOptions() =>
    [
        new(AppResources.Get("CurrencyKind_Iso4217"), CurrencyKind.Iso4217),
        new(AppResources.Get("CurrencyKind_Custom"), CurrencyKind.Custom)
    ];

    private static SelectionOption<SubscriptionConfirmationStatus>[] CreateConfirmationOptions() =>
    [
        new(AppResources.Get("Confirmation_Unconfirmed"), SubscriptionConfirmationStatus.Unknown),
        new(AppResources.Get("Confirmation_Confirmed"), SubscriptionConfirmationStatus.ConfirmedActive),
        new(AppResources.Get("Confirmation_Inactive"), SubscriptionConfirmationStatus.ConfirmedInactive)
    ];

    private static SelectionOption<SubscriptionLifecycleStatus>[] CreateLifecycleOptions() =>
    [
        new(AppResources.Get("Lifecycle_Unknown"), SubscriptionLifecycleStatus.Unknown),
        new(AppResources.Get("Lifecycle_Trial"), SubscriptionLifecycleStatus.Trial),
        new(AppResources.Get("Lifecycle_Active"), SubscriptionLifecycleStatus.Active),
        new(AppResources.Get("Lifecycle_Paused"), SubscriptionLifecycleStatus.Paused),
        new(AppResources.Get("Lifecycle_CancellationScheduled"), SubscriptionLifecycleStatus.CancellationScheduled),
        new(AppResources.Get("Lifecycle_Cancelled"), SubscriptionLifecycleStatus.Cancelled),
        new(AppResources.Get("Lifecycle_Expired"), SubscriptionLifecycleStatus.Expired)
    ];

    private static SelectionOption<SubscriptionImportance>[] CreateImportanceOptions() =>
    [
        new(AppResources.Get("Importance_Low"), SubscriptionImportance.Low),
        new(AppResources.Get("Importance_Normal"), SubscriptionImportance.Normal),
        new(AppResources.Get("Importance_Important"), SubscriptionImportance.Important),
        new(AppResources.Get("Importance_Essential"), SubscriptionImportance.Essential)
    ];

    private static SelectionOption<PhoneNumberType>[] CreatePhoneNumberTypeOptions() =>
    [
        new(AppResources.Get("PhoneType_Mobile"), PhoneNumberType.Mobile),
        new(AppResources.Get("PhoneType_Landline"), PhoneNumberType.Landline),
        new(AppResources.Get("PhoneType_Voip"), PhoneNumberType.VoiceOverIp),
        new(AppResources.Get("PhoneType_DataOnly"), PhoneNumberType.DataOnly),
        new(AppResources.Get("PhoneType_Other"), PhoneNumberType.Other)
    ];

    private static SelectionOption<CloudBillingMode>[] CreateCloudBillingModeOptions() =>
    [
        new(AppResources.Get("CloudBilling_Fixed"), CloudBillingMode.Fixed),
        new(AppResources.Get("CloudBilling_UsageBasedEstimate"), CloudBillingMode.UsageBasedEstimate)
    ];
}
