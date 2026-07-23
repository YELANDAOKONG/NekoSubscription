using System;

using CommunityToolkit.Mvvm.Input;

namespace NekoSubscription.ViewModels;

public sealed class ForecastPeriodOptionViewModel : ViewModelBase
{
    private bool _isSelected;

    public ForecastPeriodOptionViewModel(int dayCount, string label, Action<int> select)
    {
        if (dayCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dayCount), dayCount, "The forecast day count must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(select);

        DayCount = dayCount;
        Label = label;
        SelectCommand = new RelayCommand(() => select(dayCount));
    }

    public int DayCount { get; }

    public string Label { get; }

    public bool IsSelected
    {
        get => _isSelected;
        private set => SetProperty(ref _isSelected, value);
    }

    public IRelayCommand SelectCommand { get; }

    public void SetSelected(bool isSelected) => IsSelected = isSelected;
}
