using System;
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

public partial class PaymentAndTagsViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly ISubscriptionService _subscriptionService;

    public PaymentAndTagsViewModel(ISubscriptionService subscriptionService, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(subscriptionService);
        ArgumentNullException.ThrowIfNull(logger);

        _subscriptionService = subscriptionService;
        _logger = logger;
        RefreshPaymentChannels();
    }

    public event Action<string>? StatusChanged;

    public ObservableCollection<PaymentProfile> PaymentProfiles { get; } = [];

    public ObservableCollection<Tag> Tags { get; } = [];

    public ObservableCollection<SelectionOption<PaymentChannel>> PaymentChannels { get; } = [];

    public bool HasSelectedPaymentProfile => SelectedPaymentProfile is not null;

    public bool HasSelectedTag => SelectedTag is not null;

    public string PaymentArchiveActionLabel => SelectedPaymentProfile?.IsArchived == true
        ? AppResources.Get("Settings_RestorePaymentProfile")
        : AppResources.Get("Settings_ArchivePaymentProfile");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedPaymentProfile))]
    [NotifyPropertyChangedFor(nameof(PaymentArchiveActionLabel))]
    public partial PaymentProfile? SelectedPaymentProfile { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedTag))]
    public partial Tag? SelectedTag { get; set; }

    [ObservableProperty]
    public partial string PaymentDisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaymentProviderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PaymentAccountIdentifier { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SelectionOption<PaymentChannel> SelectedPaymentChannel { get; set; } = null!;

    [ObservableProperty]
    public partial string TagName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var selectedPaymentId = SelectedPaymentProfile?.Id;
            var selectedTagId = SelectedTag?.Id;
            Replace(PaymentProfiles, await _subscriptionService.GetPaymentProfilesAsync(true));
            Replace(Tags, await _subscriptionService.GetTagsAsync());
            SelectedPaymentProfile = PaymentProfiles.FirstOrDefault(profile => profile.Id == selectedPaymentId);
            SelectedTag = Tags.FirstOrDefault(tag => tag.Id == selectedTagId);
            LoadPaymentFields();
            TagName = SelectedTag?.Name ?? string.Empty;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to load payment methods and tags.");
            StatusChanged?.Invoke(AppResources.Get("Status_LoadReferencesFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void RefreshPaymentLocalization() => RefreshPaymentChannels();

    [RelayCommand]
    private async Task SavePaymentProfileAsync()
    {
        if (IsBusy || SelectedPaymentChannel is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (SelectedPaymentProfile is { } paymentProfile)
            {
                await _subscriptionService.UpdatePaymentProfileAsync(
                    paymentProfile.Id,
                    (profile, changedAtUtc) => profile.Update(
                        PaymentDisplayName,
                        SelectedPaymentChannel.Value,
                        PaymentAccountIdentifier,
                        PaymentProviderName,
                        profile.Notes,
                        changedAtUtc));
                StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileUpdated"));
            }
            else
            {
                await _subscriptionService.AddPaymentProfileAsync(new PaymentProfile(
                    PaymentDisplayName,
                    SelectedPaymentChannel.Value,
                    PaymentAccountIdentifier,
                    PaymentProviderName,
                    null));
                StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileAdded"));
            }

            await RefreshAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to save a payment method.");
            StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TogglePaymentProfileArchiveAsync()
    {
        if (SelectedPaymentProfile is not { } paymentProfile || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var changed = paymentProfile.IsArchived
                ? await _subscriptionService.RestorePaymentProfileFromArchiveAsync(paymentProfile.Id)
                : await _subscriptionService.ArchivePaymentProfileAsync(paymentProfile.Id);
            StatusChanged?.Invoke(changed
                ? AppResources.Get("Status_PaymentProfileUpdated")
                : AppResources.Get("Status_PaymentProfileFailed"));
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to update the payment method archive state.");
            StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveTagAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (SelectedTag is { } tag)
            {
                await _subscriptionService.RenameTagAsync(tag.Id, TagName);
                StatusChanged?.Invoke(AppResources.Get("Status_TagUpdated"));
            }
            else
            {
                await _subscriptionService.AddTagAsync(new Tag(TagName));
                StatusChanged?.Invoke(AppResources.Get("Status_TagAdded"));
            }

            await RefreshAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to save a tag.");
            StatusChanged?.Invoke(AppResources.Get("Status_TagFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedPaymentProfileChanged(PaymentProfile? value) => LoadPaymentFields();

    partial void OnSelectedTagChanged(Tag? value) => TagName = value?.Name ?? string.Empty;

    private void LoadPaymentFields()
    {
        PaymentDisplayName = SelectedPaymentProfile?.DisplayName ?? string.Empty;
        PaymentProviderName = SelectedPaymentProfile?.ProviderName ?? string.Empty;
        PaymentAccountIdentifier = SelectedPaymentProfile?.AccountIdentifier ?? string.Empty;
        SelectedPaymentChannel = PaymentChannels.First(option =>
            option.Value == (SelectedPaymentProfile?.Channel ?? PaymentChannel.Direct));
    }

    private void RefreshPaymentChannels()
    {
        Replace(
            PaymentChannels,
            Enum.GetValues<PaymentChannel>().Select(channel =>
                new SelectionOption<PaymentChannel>(FormatPaymentChannel(channel), channel)));
        LoadPaymentFields();
    }

    private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private static string FormatPaymentChannel(PaymentChannel channel) => channel switch
    {
        PaymentChannel.Direct => AppResources.Get("Payment_Direct"),
        PaymentChannel.AppleAppStore => AppResources.Get("Payment_AppleAppStore"),
        PaymentChannel.GooglePlay => AppResources.Get("Payment_GooglePlay"),
        PaymentChannel.PayPal => AppResources.Get("Payment_PayPal"),
        PaymentChannel.BankTransfer => AppResources.Get("Payment_BankTransfer"),
        PaymentChannel.CreditCard => AppResources.Get("Payment_CreditCard"),
        PaymentChannel.DebitCard => AppResources.Get("Payment_DebitCard"),
        PaymentChannel.Cash => AppResources.Get("Payment_Cash"),
        PaymentChannel.Other => AppResources.Get("Payment_Other"),
        _ => AppResources.Get("Common_Unknown")
    };
}
