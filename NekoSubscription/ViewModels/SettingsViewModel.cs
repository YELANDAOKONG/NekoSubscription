using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Localization;

namespace NekoSubscription.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly IApplicationSettingsService _settingsService;
    private ApplicationSettings _settings = new();
    private bool _isApplyingSettings;

    public SettingsViewModel(IApplicationSettingsService settingsService, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(logger);

        _settingsService = settingsService;
        _logger = logger;
        RefreshLocalizedOptions(
            ApplicationTheme.System,
            ApplicationVisualStyle.Standard,
            null);
    }

    public event EventHandler? AppearanceChanged;

    public event EventHandler? CultureChanged;

    public event Action<string>? StatusChanged;

    public ObservableCollection<SelectionOption<ApplicationTheme>> Themes { get; } = [];

    public ObservableCollection<SelectionOption<ApplicationVisualStyle>> VisualStyles { get; } = [];

    public ObservableCollection<SelectionOption<string?>> Languages { get; } = [];

    public ApplicationTheme SelectedTheme => SelectedThemeOption.Value;

    public ApplicationVisualStyle SelectedVisualStyle => SelectedVisualStyleOption.Value;

    public bool IsAcrylicSelected => SelectedVisualStyle == ApplicationVisualStyle.Acrylic;

    public string AcrylicOpacityLabel => $"{AcrylicOpacity:P0}";

    [ObservableProperty]
    public partial double AcrylicOpacity { get; set; } = ApplicationSettings.DefaultAcrylicOpacity;

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; private set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial SelectionOption<string?> SelectedLanguageOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<ApplicationTheme> SelectedThemeOption { get; set; } = null!;

    [ObservableProperty]
    public partial SelectionOption<ApplicationVisualStyle> SelectedVisualStyleOption { get; set; } = null!;

    public void Initialize(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _isApplyingSettings = true;
        AcrylicOpacity = settings.AcrylicOpacity;
        RefreshLocalizedOptions(
            settings.Theme,
            settings.VisualStyle,
            settings.CultureName);
        HasUnsavedChanges = false;
        _isApplyingSettings = false;
        AppearanceChanged?.Invoke(this, EventArgs.Empty);
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
            _settings.Theme = SelectedTheme;
            _settings.VisualStyle = SelectedVisualStyle;
            _settings.AcrylicOpacity = AcrylicOpacity;
            _settings.CultureName = SelectedLanguageOption.Value;
            await _settingsService.SaveAsync(_settings);
            HasUnsavedChanges = false;
            StatusChanged?.Invoke(AppResources.Get("Status_SettingsSaved"));
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to save application settings.");
            StatusChanged?.Invoke(AppResources.Get("Status_SettingsSaveFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnAcrylicOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(AcrylicOpacityLabel));
        MarkAppearanceChanged();
    }

    partial void OnSelectedLanguageOptionChanged(SelectionOption<string?> value)
    {
        if (_isApplyingSettings || value is null)
        {
            return;
        }

        AppResources.SetCulture(value.Value);
        HasUnsavedChanges = true;

        // Avalonia is still committing the ComboBox selection here. Replacing its
        // items on the next dispatcher turn keeps the selection model consistent.
        Dispatcher.UIThread.Post(() =>
        {
            RefreshLocalizedOptions(SelectedTheme, SelectedVisualStyle, value.Value);
            CultureChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    partial void OnSelectedThemeOptionChanged(SelectionOption<ApplicationTheme> value)
    {
        OnPropertyChanged(nameof(SelectedTheme));
        MarkAppearanceChanged();
    }

    partial void OnSelectedVisualStyleOptionChanged(SelectionOption<ApplicationVisualStyle> value)
    {
        OnPropertyChanged(nameof(SelectedVisualStyle));
        OnPropertyChanged(nameof(IsAcrylicSelected));
        MarkAppearanceChanged();
    }

    private void RefreshLocalizedOptions(
        ApplicationTheme selectedTheme,
        ApplicationVisualStyle selectedVisualStyle,
        string? selectedCultureName)
    {
        var wasApplyingSettings = _isApplyingSettings;
        _isApplyingSettings = true;

        ReplaceOptions(
            Themes,
            [
                new SelectionOption<ApplicationTheme>(AppResources.Get("Theme_System"), ApplicationTheme.System),
                new SelectionOption<ApplicationTheme>(AppResources.Get("Theme_Light"), ApplicationTheme.Light),
                new SelectionOption<ApplicationTheme>(AppResources.Get("Theme_Dark"), ApplicationTheme.Dark)
            ]);
        ReplaceOptions(
            VisualStyles,
            [
                new SelectionOption<ApplicationVisualStyle>(
                    AppResources.Get("VisualStyle_Standard"),
                    ApplicationVisualStyle.Standard),
                new SelectionOption<ApplicationVisualStyle>(
                    AppResources.Get("VisualStyle_Acrylic"),
                    ApplicationVisualStyle.Acrylic)
            ]);
        ReplaceOptions(
            Languages,
            [
                new SelectionOption<string?>(
                    AppResources.Get("Language_Automatic"),
                    null),
                new SelectionOption<string?>(
                    AppResources.Get("Language_English"),
                    AppResources.EnglishCultureName),
                new SelectionOption<string?>(
                    AppResources.Get("Language_SimplifiedChinese"),
                    AppResources.SimplifiedChineseCultureName),
                new SelectionOption<string?>(
                    AppResources.Get("Language_TraditionalChinese"),
                    AppResources.TraditionalChineseCultureName)
            ]);

        SelectedThemeOption = Themes.Single(option => option.Value == selectedTheme);
        SelectedVisualStyleOption = VisualStyles.Single(option => option.Value == selectedVisualStyle);
        SelectedLanguageOption = Languages.Single(option => option.Value == selectedCultureName);
        _isApplyingSettings = wasApplyingSettings;
    }

    private void MarkAppearanceChanged()
    {
        if (_isApplyingSettings)
        {
            return;
        }

        HasUnsavedChanges = true;
        AppearanceChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ReplaceOptions<T>(
        ObservableCollection<SelectionOption<T>> target,
        System.Collections.Generic.IEnumerable<SelectionOption<T>> options)
    {
        target.Clear();

        foreach (var option in options)
        {
            target.Add(option);
        }
    }
}
