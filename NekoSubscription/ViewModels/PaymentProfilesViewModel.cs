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

public partial class PaymentProfilesViewModel : ViewModelBase
{
    private readonly ISubscriptionService _service;
    private readonly ILogger _logger;

    public PaymentProfilesViewModel(ISubscriptionService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
        RefreshChannels();
    }

    public event Action<string>? StatusChanged;

    public ObservableCollection<PaymentProfile> Profiles { get; } = [];

    public ObservableCollection<SelectionOption<PaymentChannel>> Channels { get; } = [];

    public bool HasSelection => SelectedProfile is not null;

    public bool HasEditor => HasSelection || IsAdding;

    public string EditorTitle => IsAdding
        ? AppResources.Get("Settings_AddPaymentProfile")
        : DisplayName;

    public string EditorSubtitle => IsAdding
        ? AppResources.Get("Page_PaymentAndTagsSubtitle")
        : ProviderName;

    public string ArchiveActionLabel => SelectedProfile?.IsArchived == true
        ? AppResources.Get("Settings_RestorePaymentProfile")
        : AppResources.Get("Settings_ArchivePaymentProfile");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(HasEditor))]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    [NotifyPropertyChangedFor(nameof(EditorSubtitle))]
    [NotifyPropertyChangedFor(nameof(ArchiveActionLabel))]
    public partial PaymentProfile? SelectedProfile { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEditor))]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    [NotifyPropertyChangedFor(nameof(EditorSubtitle))]
    public partial bool IsAdding { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorSubtitle))]
    public partial string ProviderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AccountIdentifier { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SelectionOption<PaymentChannel> SelectedChannel { get; set; } = null!;

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
            var selectedId = SelectedProfile?.Id;
            Profiles.Clear();
            foreach (var profile in await _service.GetPaymentProfilesAsync(true))
            {
                Profiles.Add(profile);
            }

            SelectedProfile = Profiles.FirstOrDefault(profile => profile.Id == selectedId);
            LoadFields();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to load payment methods.");
            StatusChanged?.Invoke(AppResources.Get("Status_LoadReferencesFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddPaymentProfile()
    {
        if (!IsBusy)
        {
            SelectedProfile = null;
            IsAdding = true;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        if (IsBusy)
        {
            return;
        }

        IsAdding = false;
        SelectedProfile = null;
        LoadFields();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (SelectedProfile is { } profile)
            {
                await _service.UpdatePaymentProfileAsync(profile.Id, (current, changedAtUtc) => current.Update(
                    DisplayName, SelectedChannel.Value, AccountIdentifier, ProviderName, current.Notes, changedAtUtc));
                StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileUpdated"));
            }
            else
            {
                await _service.AddPaymentProfileAsync(new PaymentProfile(
                    DisplayName, SelectedChannel.Value, AccountIdentifier, ProviderName, null));
                StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileAdded"));
            }

            IsBusy = false;
            IsAdding = false;
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to save a payment method.");
            StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileFailed"));
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleArchiveAsync()
    {
        if (SelectedProfile is not { } profile || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var changed = profile.IsArchived
                ? await _service.RestorePaymentProfileFromArchiveAsync(profile.Id)
                : await _service.ArchivePaymentProfileAsync(profile.Id);
            StatusChanged?.Invoke(changed
                ? AppResources.Get("Status_PaymentProfileUpdated")
                : AppResources.Get("Status_PaymentProfileFailed"));
            IsBusy = false;
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to update a payment method.");
            StatusChanged?.Invoke(AppResources.Get("Status_PaymentProfileFailed"));
            IsBusy = false;
        }
    }

    public void RefreshLocalization()
    {
        RefreshChannels();
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(EditorSubtitle));
    }

    partial void OnSelectedProfileChanged(PaymentProfile? value)
    {
        if (value is not null)
        {
            IsAdding = false;
        }

        LoadFields();
    }

    private void LoadFields()
    {
        DisplayName = SelectedProfile?.DisplayName ?? string.Empty;
        ProviderName = SelectedProfile?.ProviderName ?? string.Empty;
        AccountIdentifier = SelectedProfile?.AccountIdentifier ?? string.Empty;
        SelectedChannel = Channels.First(option => option.Value == (SelectedProfile?.Channel ?? PaymentChannel.Direct));
    }

    private void RefreshChannels()
    {
        Channels.Clear();
        foreach (var channel in Enum.GetValues<PaymentChannel>())
        {
            Channels.Add(new SelectionOption<PaymentChannel>(FormatChannel(channel), channel));
        }

        LoadFields();
    }

    private static string FormatChannel(PaymentChannel channel) => channel switch
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
