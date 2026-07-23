using System;
using System.Globalization;

using NekoSubscription.Entities.Subscriptions;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public sealed record SubscriptionListItemViewModel(
    Guid Id,
    string ServiceLabel,
    string ProviderLabel,
    string AccountLabel,
    SubscriptionCategory Category,
    SubscriptionLifecycleStatus LifecycleStatus,
    string CategoryLabel,
    string StatusLabel,
    string LifecycleLabel,
    string AmountLabel,
    string NextBillingLabel,
    DateOnly? NextBillingOn,
    string ImportanceLabel,
    string ScheduleLabel,
    string NotesLabel,
    string ManagementUrlLabel,
    string SpecializedDetailsLabel,
    bool ParticipatesInBudget,
    bool IsArchived)
{
    public string ArchiveStateLabel => IsArchived
        ? AppResources.Get("Details_Archived")
        : AppResources.Get("Details_Visible");

    public bool Matches(string searchText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);

        return ServiceLabel.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
            ProviderLabel.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
            AccountLabel.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
            CategoryLabel.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
            StatusLabel.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
            LifecycleLabel.Contains(searchText, StringComparison.CurrentCultureIgnoreCase);
    }

    public string BudgetStateLabel => ParticipatesInBudget
        ? AppResources.Get("Forecast_IncludedInBudget")
        : AppResources.Get("Forecast_ExcludedFromBudget");

    public static SubscriptionListItemViewModel FromSubscription(Subscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        var serviceLabel = subscription.PlanName is null
            ? subscription.ServiceName
            : $"{subscription.ServiceName} · {subscription.PlanName}";
        var nextBillingOn = subscription.BillingSchedule.NextBillingOn ??
            subscription.BillingSchedule.StartsOn;

        return new SubscriptionListItemViewModel(
            subscription.Id,
            serviceLabel,
            subscription.ProviderName,
            subscription.AccountName ?? AppResources.Get("Details_NoAccount"),
            subscription.Category,
            subscription.LifecycleStatus,
            FormatCategory(subscription.Category),
            FormatConfirmationStatus(subscription.ConfirmationStatus, subscription.IsArchived),
            FormatLifecycleStatus(subscription.LifecycleStatus),
            FormatAmount(subscription.BillingAmount),
            nextBillingOn?.ToString("d", CultureInfo.CurrentCulture) ??
                AppResources.Get("Common_NotScheduled"),
            nextBillingOn,
            FormatImportance(subscription.Importance),
            FormatSchedule(subscription.BillingSchedule),
            subscription.Notes ?? AppResources.Get("Details_NoNotes"),
            subscription.ManagementUrl ?? AppResources.Get("Details_NoManagementLink"),
            FormatSpecializedDetails(subscription),
            subscription.ParticipatesInBudget,
            subscription.IsArchived);
    }

    private static string FormatAmount(Money money)
    {
        var amount = money.Amount.ToString("0.##", CultureInfo.CurrentCulture);
        return $"{amount} {money.CurrencyCode}";
    }

    private static string FormatCategory(SubscriptionCategory category) => category switch
    {
        SubscriptionCategory.Ordinary => AppResources.Get("Category_Ordinary"),
        SubscriptionCategory.PhoneNumber => AppResources.Get("Category_PhoneNumber"),
        SubscriptionCategory.Domain => AppResources.Get("Category_Domain"),
        SubscriptionCategory.CloudService => AppResources.Get("Category_CloudService"),
        SubscriptionCategory.Custom => AppResources.Get("Category_Custom"),
        _ => AppResources.Get("Common_Unknown")
    };

    private static string FormatConfirmationStatus(
        SubscriptionConfirmationStatus status,
        bool isArchived)
    {
        if (isArchived)
        {
            return AppResources.Get("Details_Archived");
        }

        return status switch
        {
            SubscriptionConfirmationStatus.ConfirmedActive => AppResources.Get("Confirmation_Confirmed"),
            SubscriptionConfirmationStatus.ConfirmedInactive => AppResources.Get("Confirmation_Inactive"),
            SubscriptionConfirmationStatus.Unknown => AppResources.Get("Confirmation_Unconfirmed"),
            _ => AppResources.Get("Common_Unknown")
        };
    }

    private static string FormatLifecycleStatus(SubscriptionLifecycleStatus status) => status switch
    {
        SubscriptionLifecycleStatus.Unknown => AppResources.Get("Lifecycle_Unknown"),
        SubscriptionLifecycleStatus.Trial => AppResources.Get("Lifecycle_Trial"),
        SubscriptionLifecycleStatus.Active => AppResources.Get("Lifecycle_Active"),
        SubscriptionLifecycleStatus.Paused => AppResources.Get("Lifecycle_Paused"),
        SubscriptionLifecycleStatus.CancellationScheduled =>
            AppResources.Get("Lifecycle_CancellationScheduled"),
        SubscriptionLifecycleStatus.Cancelled => AppResources.Get("Lifecycle_Cancelled"),
        SubscriptionLifecycleStatus.Expired => AppResources.Get("Lifecycle_Expired"),
        _ => AppResources.Get("Lifecycle_Unknown")
    };

    private static string FormatImportance(SubscriptionImportance importance) => importance switch
    {
        SubscriptionImportance.Low => AppResources.Get("Importance_Low"),
        SubscriptionImportance.Normal => AppResources.Get("Importance_Normal"),
        SubscriptionImportance.Important => AppResources.Get("Importance_Important"),
        SubscriptionImportance.Essential => AppResources.Get("Importance_Essential"),
        _ => AppResources.Get("Common_Unknown")
    };

    private static string FormatSchedule(BillingSchedule schedule)
    {
        if (schedule.Cadence != BillingCadence.Recurring)
        {
            return schedule.Cadence == BillingCadence.OneTime
                ? AppResources.Get("Schedule_OneTime")
                : AppResources.Get("Schedule_Manual");
        }

        var intervalUnit = FormatIntervalUnit(schedule.IntervalUnit, schedule.IntervalCount != 1);
        return schedule.IntervalCount == 1
            ? AppResources.Format("Schedule_EveryOne", intervalUnit)
            : AppResources.Format("Schedule_EveryMany", schedule.IntervalCount, intervalUnit);
    }

    private static string FormatIntervalUnit(BillingIntervalUnit? intervalUnit, bool plural) =>
        intervalUnit switch
        {
            BillingIntervalUnit.Day => AppResources.Get(plural ? "Interval_Days" : "Interval_Day"),
            BillingIntervalUnit.Week => AppResources.Get(plural ? "Interval_Weeks" : "Interval_Week"),
            BillingIntervalUnit.Month => AppResources.Get(plural ? "Interval_Months" : "Interval_Month"),
            BillingIntervalUnit.Year => AppResources.Get(plural ? "Interval_Years" : "Interval_Year"),
            _ => AppResources.Get("Common_Unknown")
        };

    private static string FormatSpecializedDetails(Subscription subscription) => subscription switch
    {
        PhoneNumberSubscription phone => AppResources.Format(
            "Details_Phone",
            phone.PhoneNumber,
            phone.CarrierName),
        DomainSubscription domain => domain.ExpiresOn is { } expiresOn
            ? AppResources.Format(
                "Details_DomainExpires",
                domain.DomainName,
                expiresOn.ToString("d", CultureInfo.CurrentCulture))
            : domain.DomainName,
        CloudServiceSubscription cloud => AppResources.Format(
            "Details_CloudBilling",
            FormatCloudBillingMode(cloud.BillingMode)),
        CustomSubscription custom => AppResources.Format("Details_CustomFields", custom.Fields.Count),
        OrdinarySubscription => AppResources.Get("Details_Ordinary"),
        _ => AppResources.Get("Details_Generic")
    };

    private static string FormatCloudBillingMode(CloudBillingMode billingMode) => billingMode switch
    {
        CloudBillingMode.Fixed => AppResources.Get("CloudBilling_Fixed"),
        CloudBillingMode.UsageBasedEstimate => AppResources.Get("CloudBilling_UsageBasedEstimate"),
        _ => AppResources.Get("Common_Unknown")
    };
}
