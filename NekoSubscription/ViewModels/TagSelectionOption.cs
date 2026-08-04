using CommunityToolkit.Mvvm.ComponentModel;

using NekoSubscription.Entities.Subscriptions;

namespace NekoSubscription.ViewModels;

public sealed partial class TagSelectionOption : ObservableObject
{
    public TagSelectionOption(Tag tag, bool isSelected)
    {
        Tag = tag;
        IsSelected = isSelected;
    }

    public Tag Tag { get; }

    public string Name => Tag.Name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
