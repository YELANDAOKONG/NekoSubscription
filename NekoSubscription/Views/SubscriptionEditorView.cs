using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;

using NekoSubscription.Localization;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public sealed class SubscriptionEditorView : UserControl
{
    public SubscriptionEditorView()
    {
        var form = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 14,
                Margin = new Thickness(0, 0, 8, 18),
                MaxWidth = 960
            }
            .Children(
                BuildHeader(),
                BuildIdentitySection(),
                BuildBillingSection(),
                BuildStatusSection(),
                BuildPhoneSection(),
                BuildDomainSection(),
                BuildCloudSection(),
                BuildNotesSection())
        };
        form.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsReady)));

        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto")
        }
        .Children(
            form.Grid_Row(0),
            BuildFooter().Grid_Row(1));
    }

    private static Control BuildHeader()
    {
        return new StackPanel
        {
            Spacing = 4
        }
        .Children(
            UiFactory.BoundText(nameof(SubscriptionEditorViewModel.Title), 22, FontWeight.SemiBold),
            new TextBlock
            {
                Text = AppResources.Get("Editor_Description"),
                Opacity = 0.64,
                TextWrapping = TextWrapping.Wrap
            });
    }

    private static Control BuildIdentitySection()
    {
        var category = BuildComboBox(
            nameof(SubscriptionEditorViewModel.SubscriptionCategories),
            nameof(SubscriptionEditorViewModel.SelectedCategoryOption));
        category.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionEditorViewModel.CanChangeCategory)));

        return BuildSection(
            AppResources.Get("Editor_IdentitySection"),
            BuildField(AppResources.Get("Editor_Category"), category),
            BuildField(
                AppResources.Get("Editor_Provider"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.ProviderName))),
            BuildField(
                AppResources.Get("Editor_Service"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.ServiceName))),
            BuildField(
                AppResources.Get("Editor_Plan"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.PlanName))),
            BuildField(
                AppResources.Get("Editor_Account"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.AccountName))));
    }

    private static Control BuildBillingSection()
    {
        var amount = new NumericUpDown
        {
            Minimum = 0,
            Maximum = decimal.MaxValue,
            Increment = 1,
            FormatString = "0.00"
        };
        amount.Bind(
            NumericUpDown.ValueProperty,
            new Binding(nameof(SubscriptionEditorViewModel.Amount))
            {
                Mode = BindingMode.TwoWay
            });

        var intervalCount = new NumericUpDown
        {
            Minimum = 1,
            Maximum = int.MaxValue,
            Increment = 1,
            FormatString = "0"
        };
        intervalCount.Bind(
            NumericUpDown.ValueProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IntervalCount))
            {
                Mode = BindingMode.TwoWay
            });

        var intervalField = BuildField(
            AppResources.Get("Editor_Interval"),
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                ColumnSpacing = 10
            }
            .Children(
                intervalCount,
                BuildComboBox(
                        nameof(SubscriptionEditorViewModel.BillingIntervalUnits),
                        nameof(SubscriptionEditorViewModel.SelectedBillingIntervalUnitOption))
                    .Grid_Column(1)));
        intervalField.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsRecurring)));

        var renews = new CheckBox
        {
            Content = AppResources.Get("Editor_AutomaticallyRenews")
        };
        renews.Bind(
            Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding(nameof(SubscriptionEditorViewModel.AutomaticallyRenews))
            {
                Mode = BindingMode.TwoWay
            });
        var renewsField = BuildField(string.Empty, renews);
        renewsField.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsRecurring)));

        return BuildSection(
            AppResources.Get("Editor_BillingSection"),
            BuildField(AppResources.Get("Editor_Amount"), amount),
            BuildField(
                AppResources.Get("Editor_CurrencyCode"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.CurrencyCode))),
            BuildField(
                AppResources.Get("Editor_CurrencyKind"),
                BuildComboBox(
                    nameof(SubscriptionEditorViewModel.CurrencyKinds),
                    nameof(SubscriptionEditorViewModel.SelectedCurrencyKindOption))),
            BuildField(
                AppResources.Get("Editor_Cadence"),
                BuildComboBox(
                    nameof(SubscriptionEditorViewModel.BillingCadences),
                    nameof(SubscriptionEditorViewModel.SelectedBillingCadenceOption))),
            intervalField,
            BuildField(
                AppResources.Get("Editor_StartDate"),
                BuildDatePicker(nameof(SubscriptionEditorViewModel.StartsOn))),
            BuildField(
                AppResources.Get("Editor_NextBillingDate"),
                BuildDatePicker(nameof(SubscriptionEditorViewModel.NextBillingOn))),
            BuildField(
                AppResources.Get("Editor_EndDate"),
                BuildDatePicker(nameof(SubscriptionEditorViewModel.EndsOn))),
            renewsField);
    }

    private static Control BuildStatusSection()
    {
        return BuildSection(
            AppResources.Get("Editor_StatusSection"),
            BuildField(
                AppResources.Get("Editor_Confirmation"),
                BuildComboBox(
                    nameof(SubscriptionEditorViewModel.ConfirmationStatuses),
                    nameof(SubscriptionEditorViewModel.SelectedConfirmationStatusOption))),
            BuildField(
                AppResources.Get("Editor_Lifecycle"),
                BuildComboBox(
                    nameof(SubscriptionEditorViewModel.LifecycleStatuses),
                    nameof(SubscriptionEditorViewModel.SelectedLifecycleStatusOption))),
            BuildField(
                AppResources.Get("Editor_Importance"),
                BuildComboBox(
                    nameof(SubscriptionEditorViewModel.ImportanceOptions),
                    nameof(SubscriptionEditorViewModel.SelectedImportanceOption))));
    }

    private static Control BuildPhoneSection()
    {
        var prepaid = new CheckBox
        {
            Content = AppResources.Get("Editor_Prepaid")
        };
        prepaid.Bind(
            Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsPrepaid))
            {
                Mode = BindingMode.TwoWay
            });

        var section = BuildSection(
            AppResources.Get("Editor_PhoneSection"),
            BuildField(
                AppResources.Get("Editor_PhoneNumber"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.PhoneNumber))),
            BuildField(
                AppResources.Get("Editor_PhoneType"),
                BuildComboBox(
                    nameof(SubscriptionEditorViewModel.PhoneNumberTypes),
                    nameof(SubscriptionEditorViewModel.SelectedPhoneNumberTypeOption))),
            BuildField(
                AppResources.Get("Editor_Carrier"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.CarrierName))),
            BuildField(
                AppResources.Get("Editor_Region"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.RegionName))),
            BuildField(string.Empty, prepaid));
        section.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsPhoneNumber)));
        return section;
    }

    private static Control BuildDomainSection()
    {
        var section = BuildSection(
            AppResources.Get("Editor_DomainSection"),
            BuildField(
                AppResources.Get("Editor_DomainName"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.DomainName))),
            BuildField(
                AppResources.Get("Editor_RegisteredDate"),
                BuildDatePicker(nameof(SubscriptionEditorViewModel.DomainRegisteredOn))),
            BuildField(
                AppResources.Get("Editor_ExpirationDate"),
                BuildDatePicker(nameof(SubscriptionEditorViewModel.DomainExpiresOn))));
        section.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsDomain)));
        return section;
    }

    private static Control BuildCloudSection()
    {
        var section = BuildSection(
            AppResources.Get("Editor_CloudSection"),
            BuildField(
                AppResources.Get("Editor_CloudBillingMode"),
                BuildComboBox(
                    nameof(SubscriptionEditorViewModel.CloudBillingModes),
                    nameof(SubscriptionEditorViewModel.SelectedCloudBillingModeOption))),
            BuildField(
                AppResources.Get("Editor_Tenant"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.TenantIdentifier))),
            BuildField(
                AppResources.Get("Editor_Project"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.ProjectIdentifier))));
        section.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsCloudService)));
        return section;
    }

    private static Control BuildNotesSection()
    {
        var notes = BuildTextBox(nameof(SubscriptionEditorViewModel.Notes));
        notes.AcceptsReturn = true;
        notes.MinHeight = 88;
        notes.TextWrapping = TextWrapping.Wrap;

        return BuildSection(
            AppResources.Get("Editor_NotesSection"),
            BuildField(AppResources.Get("Editor_Notes"), notes),
            BuildField(
                AppResources.Get("Editor_ManagementUrl"),
                BuildTextBox(nameof(SubscriptionEditorViewModel.ManagementUrl))));
    }

    private static Control BuildError()
    {
        var error = new Border
        {
            Background = UiPalette.DangerSurface,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12),
            Child = UiFactory.BoundText(
                nameof(SubscriptionEditorViewModel.ErrorMessage),
                12,
                textWrapping: TextWrapping.Wrap)
        };
        error.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.HasError)));
        return error;
    }

    private static Control BuildActions()
    {
        var cancel = new Button
        {
            Content = AppResources.Get("Common_Cancel"),
            MinWidth = 100
        };
        cancel.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionEditorViewModel.CancelCommand)));
        cancel.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsReady)));

        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionEditorViewModel.SaveCommand)));
        save.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsReady)));

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        }
        .Children(cancel, save);
    }

    private static Control BuildFooter()
    {
        var progress = new ProgressBar
        {
            Height = 2,
            IsIndeterminate = true
        };
        progress.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsBusy)));

        return new Border
        {
            Background = UiPalette.Surface,
            BorderBrush = UiPalette.Border,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(12, 10, 20, 12),
            Child = new StackPanel
            {
                Spacing = 8
            }
            .Children(
                BuildError(),
                progress,
                BuildActions())
        };
    }

    private static Border BuildSection(string title, params Control[] fields)
    {
        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 11
            }
            .Children(
                UiFactory.SectionTitle(title),
                new StackPanel
                {
                    Spacing = 9
                }
                .Children(fields)));
    }

    private static Control BuildField(string label, Control editor)
    {
        Control labelControl = string.IsNullOrEmpty(label)
            ? new Border()
            : new Label
            {
                Content = label,
                Target = editor,
                FontWeight = FontWeight.Medium,
                Padding = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center
            };

        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("190,*"),
            ColumnSpacing = 14
        }
        .Children(
            labelControl,
            editor.Grid_Column(1));
    }

    private static TextBox BuildTextBox(string propertyPath)
    {
        var textBox = new TextBox();
        textBox.Bind(
            TextBox.TextProperty,
            new Binding(propertyPath)
            {
                Mode = BindingMode.TwoWay
            });
        return textBox;
    }

    private static ComboBox BuildComboBox(string itemsPath, string selectedItemPath)
    {
        var comboBox = new ComboBox();
        comboBox.Bind(
            ItemsControl.ItemsSourceProperty,
            new Binding(itemsPath));
        comboBox.Bind(
            Avalonia.Controls.Primitives.SelectingItemsControl.SelectedItemProperty,
            new Binding(selectedItemPath)
            {
                Mode = BindingMode.TwoWay
            });
        return comboBox;
    }

    private static DatePicker BuildDatePicker(string propertyPath)
    {
        var datePicker = new DatePicker();
        datePicker.Bind(
            DatePicker.SelectedDateProperty,
            new Binding(propertyPath)
            {
                Mode = BindingMode.TwoWay
            });
        return datePicker;
    }
}
