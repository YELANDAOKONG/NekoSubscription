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

public partial class TagsViewModel : ViewModelBase
{
    private readonly ISubscriptionService _service;
    private readonly ILogger _logger;

    public TagsViewModel(ISubscriptionService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
    }

    public event Action<string>? StatusChanged;

    public ObservableCollection<Tag> Tags { get; } = [];

    public bool HasSelection => SelectedTag is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    public partial Tag? SelectedTag { get; set; }

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

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
            var selectedId = SelectedTag?.Id;
            Tags.Clear();
            foreach (var tag in await _service.GetTagsAsync())
            {
                Tags.Add(tag);
            }

            SelectedTag = Tags.FirstOrDefault(tag => tag.Id == selectedId);
            Name = SelectedTag?.Name ?? string.Empty;
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to load tags.");
            StatusChanged?.Invoke(AppResources.Get("Status_LoadReferencesFailed"));
        }
        finally
        {
            IsBusy = false;
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
        try
        {
            if (SelectedTag is { } tag)
            {
                await _service.RenameTagAsync(tag.Id, Name);
                StatusChanged?.Invoke(AppResources.Get("Status_TagUpdated"));
            }
            else
            {
                await _service.AddTagAsync(new Tag(Name));
                StatusChanged?.Invoke(AppResources.Get("Status_TagAdded"));
            }

            IsBusy = false;
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to save a tag.");
            StatusChanged?.Invoke(AppResources.Get("Status_TagFailed"));
            IsBusy = false;
        }
    }

    partial void OnSelectedTagChanged(Tag? value) => Name = value?.Name ?? string.Empty;
}
