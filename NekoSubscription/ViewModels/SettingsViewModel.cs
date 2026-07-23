using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using NekoSubscription.Core.Configuration;
using NekoSubscription.Core.DataManagement;
using NekoSubscription.Localization;
using NekoSubscription.Services;

namespace NekoSubscription.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private const int MaximumDisplayedImportIssueCount = 5;
    private const int MaximumImportFileSize = 10 * 1024 * 1024;

    private readonly IDataManagementService _dataManagementService;
    private readonly IDataFileDialogService _fileDialogService;
    private readonly ILogger _logger;
    private readonly Func<Task> _subscriptionDataChanged;
    private readonly IApplicationSettingsService _settingsService;
    private byte[]? _pendingCsvData;
    private ApplicationSettings _settings = new();
    private bool _isApplyingSettings;

    public SettingsViewModel(
        IApplicationSettingsService settingsService,
        IDataManagementService dataManagementService,
        IDataFileDialogService fileDialogService,
        ILogger logger,
        Func<Task> subscriptionDataChanged)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(dataManagementService);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(subscriptionDataChanged);

        _settingsService = settingsService;
        _dataManagementService = dataManagementService;
        _fileDialogService = fileDialogService;
        _logger = logger;
        _subscriptionDataChanged = subscriptionDataChanged;
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

    public bool CanImport => ImportPreview?.CanImport == true && _pendingCsvData is not null;

    public bool HasImportIssues => !string.IsNullOrWhiteSpace(ImportIssueSummary);

    public bool HasImportPreview => ImportPreview is not null;

    [ObservableProperty]
    public partial double AcrylicOpacity { get; set; } = ApplicationSettings.DefaultAcrylicOpacity;

    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; private set; }

    [ObservableProperty]
    public partial bool IsBusy { get; private set; }

    [ObservableProperty]
    public partial bool IsClearConfirmationVisible { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImportIssues))]
    public partial string ImportIssueSummary { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanImport))]
    [NotifyPropertyChangedFor(nameof(HasImportPreview))]
    public partial CsvImportPreview? ImportPreview { get; private set; }

    [ObservableProperty]
    public partial string ImportPreviewMessage { get; private set; } = string.Empty;

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

    [RelayCommand]
    private async Task BackupDataAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (await TryCreateBackupAsync())
            {
                StatusChanged?.Invoke(AppResources.Get("Status_DataBackupCreated"));
            }
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to create an application data backup.");
            StatusChanged?.Invoke(AppResources.Get("Status_DataBackupFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectImportCsvAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearImportPreview();

        try
        {
            await using var source = await _fileDialogService.OpenCsvFileAsync(
                AppResources.Get("Settings_ImportDialogTitle"));
            if (source is null)
            {
                return;
            }

            _pendingCsvData = await ReadImportFileAsync(source);
            await using var previewSource = new MemoryStream(_pendingCsvData, writable: false);
            ImportPreview = await _dataManagementService.PreviewSubscriptionCsvAsync(previewSource);
            RefreshImportPreviewText();
        }
        catch (Exception exception)
        {
            ClearImportPreview();
            _logger.Error(exception, "Failed to preview a subscription CSV import.");
            StatusChanged?.Invoke(AppResources.Get("Status_CsvPreviewFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelImport() => ClearImportPreview();

    [RelayCommand]
    private async Task ConfirmImportAsync()
    {
        if (IsBusy || !CanImport || _pendingCsvData is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await using var source = new MemoryStream(_pendingCsvData, writable: false);
            var result = await _dataManagementService.ImportSubscriptionCsvAsync(source);
            ClearImportPreview();
            await _subscriptionDataChanged();
            StatusChanged?.Invoke(AppResources.Format(
                "Status_CsvImportCompleted",
                result.ImportedSubscriptionCount));
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to import subscriptions from CSV.");
            StatusChanged?.Invoke(AppResources.Get("Status_CsvImportFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void RequestClearData()
    {
        if (!IsBusy)
        {
            IsClearConfirmationVisible = true;
        }
    }

    [RelayCommand]
    private void CancelClearData() => IsClearConfirmationVisible = false;

    [RelayCommand]
    private async Task BackupAndClearDataAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (!await TryCreateBackupAsync())
            {
                return;
            }

            await ClearDataCoreAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to back up and clear subscription data.");
            StatusChanged?.Invoke(AppResources.Get("Status_ClearDataFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmClearDataAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await ClearDataCoreAsync();
        }
        catch (Exception exception)
        {
            _logger.Error(exception, "Failed to clear subscription data.");
            StatusChanged?.Invoke(AppResources.Get("Status_ClearDataFailed"));
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
        RefreshImportPreviewText();
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
        IEnumerable<SelectionOption<T>> options)
    {
        target.Clear();

        foreach (var option in options)
        {
            target.Add(option);
        }
    }

    private async Task<bool> TryCreateBackupAsync()
    {
        await using var destination = await _fileDialogService.CreateBackupFileAsync(
            AppResources.Get("Settings_BackupDialogTitle"),
            AppResources.Get("Settings_BackupFileType"));
        if (destination is null)
        {
            return false;
        }

        if (destination.CanSeek)
        {
            destination.SetLength(0);
        }

        await _dataManagementService.CreateBackupAsync(destination);
        await destination.FlushAsync();
        return true;
    }

    private async Task ClearDataCoreAsync()
    {
        var result = await _dataManagementService.ClearSubscriptionDataAsync();
        IsClearConfirmationVisible = false;
        ClearImportPreview();
        await _subscriptionDataChanged();
        StatusChanged?.Invoke(AppResources.Format(
            "Status_DataCleared",
            result.DeletedSubscriptionCount));
    }

    private void ClearImportPreview()
    {
        _pendingCsvData = null;
        ImportPreview = null;
        ImportPreviewMessage = string.Empty;
        ImportIssueSummary = string.Empty;
        OnPropertyChanged(nameof(CanImport));
    }

    private void RefreshImportPreviewText()
    {
        if (ImportPreview is not { } preview)
        {
            ImportPreviewMessage = string.Empty;
            ImportIssueSummary = string.Empty;
            return;
        }

        ImportPreviewMessage = AppResources.Format(
            "Settings_ImportPreviewSummary",
            preview.ValidRowCount,
            preview.ErrorCount,
            preview.WarningCount);
        ImportIssueSummary = string.Join(
            Environment.NewLine,
            preview.Issues
                .Take(MaximumDisplayedImportIssueCount)
                .Select(issue => AppResources.Format(
                    "Settings_ImportIssueLine",
                    issue.RowNumber,
                    GetImportIssueText(issue.Code))));
        OnPropertyChanged(nameof(CanImport));
    }

    private static string GetImportIssueText(CsvImportIssueCode code) => code switch
    {
        CsvImportIssueCode.MalformedCsv => AppResources.Get("ImportIssue_MalformedCsv"),
        CsvImportIssueCode.InvalidColumnCount => AppResources.Get("ImportIssue_InvalidColumnCount"),
        CsvImportIssueCode.MissingProvider => AppResources.Get("ImportIssue_MissingProvider"),
        CsvImportIssueCode.InvalidAmountOrCurrency =>
            AppResources.Get("ImportIssue_InvalidAmountOrCurrency"),
        CsvImportIssueCode.InvalidBillingPeriod =>
            AppResources.Get("ImportIssue_InvalidBillingPeriod"),
        CsvImportIssueCode.InvalidDate => AppResources.Get("ImportIssue_InvalidDate"),
        CsvImportIssueCode.InvalidDateOrder => AppResources.Get("ImportIssue_InvalidDateOrder"),
        CsvImportIssueCode.InvalidSubscriptionMarker =>
            AppResources.Get("ImportIssue_InvalidSubscriptionMarker"),
        CsvImportIssueCode.InvalidPaymentChannel =>
            AppResources.Get("ImportIssue_InvalidPaymentChannel"),
        CsvImportIssueCode.MissingPaymentAccount =>
            AppResources.Get("ImportIssue_MissingPaymentAccount"),
        CsvImportIssueCode.DuplicateRow => AppResources.Get("ImportIssue_DuplicateRow"),
        _ => AppResources.Get("Common_Unknown")
    };

    private static async Task<byte[]> ReadImportFileAsync(Stream source)
    {
        using var destination = new MemoryStream();
        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer)) > 0)
        {
            if (destination.Length + bytesRead > MaximumImportFileSize)
            {
                throw new InvalidDataException(
                    $"The CSV file cannot exceed {MaximumImportFileSize} bytes.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        return destination.ToArray();
    }
}
