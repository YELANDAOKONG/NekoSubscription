using System.Collections.Generic;

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
        var content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 10
        }
        .Children(
            BuildHeader().Grid_Row(0),
            BuildTabs().Grid_Row(1),
            BuildFooter().Grid_Row(2));
        content.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsReady)));

        Content = content;
    }

    private static Control BuildHeader()
    {
        return new StackPanel
        {
            Spacing = 3,
            Margin = new Thickness(2, 0, 2, 2)
        }
        .Children(
            UiFactory.BoundText(nameof(SubscriptionEditorViewModel.Title), 21, FontWeight.SemiBold),
            new TextBlock
            {
                Text = AppResources.Get("Editor_ProgressiveDescription"),
                FontSize = 12,
                Opacity = 0.64,
                TextWrapping = TextWrapping.Wrap
            });
    }

    private static Control BuildTabs()
    {
        return new TabControl
        {
            ItemsSource = new[]
            {
                BuildTab(AppResources.Get("Editor_BasicsTab"), BuildBasicsTab()),
                BuildTab(AppResources.Get("Editor_BillingTab"), BuildBillingTab()),
                BuildTab(AppResources.Get("Editor_StatusTab"), BuildStatusTab()),
                BuildTab(AppResources.Get("Editor_DetailsTab"), BuildDetailsTab())
            }
        };
    }

    private static TabItem BuildTab(string header, Control content)
    {
        return new TabItem
        {
            Header = header,
            Content = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                Content = content
            }
        };
    }

    private static Control BuildBasicsTab()
    {
        var category = BuildComboBox(
            nameof(SubscriptionEditorViewModel.SubscriptionCategories),
            nameof(SubscriptionEditorViewModel.SelectedCategoryOption));
        category.Bind(
            IsEnabledProperty,
            new Binding(nameof(SubscriptionEditorViewModel.CanChangeCategory)));

        var categoryHint = new Border
        {
            Background = UiPalette.AccentSurface,
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(10, 8),
            Child = new TextBlock
            {
                Text = AppResources.Get("Editor_CategoryLockedHint"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            }
        };
        categoryHint.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsEditing)));

        return BuildTabBody(
            AppResources.Get("Editor_IdentitySection"),
            AppResources.Get("Editor_BasicsHint"),
            BuildFieldGrid(
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
                    BuildTextBox(nameof(SubscriptionEditorViewModel.AccountName)))),
            categoryHint);
    }

    private static Control BuildBillingTab()
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

        var interval = BuildField(
            AppResources.Get("Editor_Interval"),
            new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                ColumnSpacing = 8
            }
            .Children(
                intervalCount,
                BuildComboBox(
                        nameof(SubscriptionEditorViewModel.BillingIntervalUnits),
                        nameof(SubscriptionEditorViewModel.SelectedBillingIntervalUnitOption))
                    .Grid_Column(1)));
        interval.Bind(
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
        var renewsField = BuildField(AppResources.Get("Editor_Renewal"), renews);
        renewsField.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsRecurring)));

        return BuildTabBody(
            AppResources.Get("Editor_BillingSection"),
            AppResources.Get("Editor_BillingHint"),
            BuildFieldGrid(
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
                interval,
                BuildField(
                    AppResources.Get("Editor_StartDate"),
                    BuildDatePicker(nameof(SubscriptionEditorViewModel.StartsOn))),
                BuildField(
                    AppResources.Get("Editor_NextBillingDate"),
                    BuildDatePicker(nameof(SubscriptionEditorViewModel.NextBillingOn))),
                BuildField(
                    AppResources.Get("Editor_EndDate"),
                    BuildDatePicker(nameof(SubscriptionEditorViewModel.EndsOn))),
                renewsField));
    }

    private static Control BuildStatusTab()
    {
        return BuildTabBody(
            AppResources.Get("Editor_StatusSection"),
            AppResources.Get("Editor_StatusHint"),
            new Border
            {
                Background = UiPalette.WarningSurface,
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(10, 8),
                Child = new TextBlock
                {
                    Text = AppResources.Get("Editor_ForecastInclusionHint"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                }
            },
            BuildFieldGrid(
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
                        nameof(SubscriptionEditorViewModel.SelectedImportanceOption)))));
    }

    private static Control BuildDetailsTab()
    {
        return BuildTabBody(
            AppResources.Get("Editor_DetailsTab"),
            AppResources.Get("Editor_DetailsHint"),
            BuildPhonePanel(),
            BuildDomainPanel(),
            BuildCloudPanel(),
            BuildNotesPanel());
    }

    private static Control BuildPhonePanel()
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

        var panel = BuildSection(
            AppResources.Get("Editor_PhoneSection"),
            BuildFieldGrid(
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
                BuildField(AppResources.Get("Editor_BillingType"), prepaid)));
        panel.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsPhoneNumber)));
        return panel;
    }

    private static Control BuildDomainPanel()
    {
        var panel = BuildSection(
            AppResources.Get("Editor_DomainSection"),
            BuildFieldGrid(
                BuildField(
                    AppResources.Get("Editor_DomainName"),
                    BuildTextBox(nameof(SubscriptionEditorViewModel.DomainName))),
                BuildField(
                    AppResources.Get("Editor_RegisteredDate"),
                    BuildDatePicker(nameof(SubscriptionEditorViewModel.DomainRegisteredOn))),
                BuildField(
                    AppResources.Get("Editor_ExpirationDate"),
                    BuildDatePicker(nameof(SubscriptionEditorViewModel.DomainExpiresOn)))));
        panel.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsDomain)));
        return panel;
    }

    private static Control BuildCloudPanel()
    {
        var panel = BuildSection(
            AppResources.Get("Editor_CloudSection"),
            BuildFieldGrid(
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
                    BuildTextBox(nameof(SubscriptionEditorViewModel.ProjectIdentifier)))));
        panel.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.IsCloudService)));
        return panel;
    }

    private static Control BuildNotesPanel()
    {
        var notes = BuildTextBox(nameof(SubscriptionEditorViewModel.Notes));
        notes.AcceptsReturn = true;
        notes.MinHeight = 80;
        notes.TextWrapping = TextWrapping.Wrap;

        return BuildSection(
            AppResources.Get("Editor_NotesSection"),
            BuildFieldGrid(
                BuildField(AppResources.Get("Editor_Notes"), notes),
                BuildField(
                    AppResources.Get("Editor_ManagementUrl"),
                    BuildTextBox(nameof(SubscriptionEditorViewModel.ManagementUrl)))));
    }

    private static Control BuildTabBody(string title, string description, params Control[] content)
    {
        var children = new List<Control>
        {
            UiFactory.SectionTitle(title),
            new TextBlock
            {
                Text = description,
                FontSize = 11,
                Opacity = 0.62,
                TextWrapping = TextWrapping.Wrap
            }
        };
        children.AddRange(content);

        return new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(4, 14, 4, 14)
        }
        .Children(children.ToArray());
    }

    private static Border BuildSection(string title, Control content)
    {
        return UiFactory.Card(
            new StackPanel
            {
                Spacing = 10
            }
            .Children(
                new TextBlock
                {
                    Text = title,
                    FontWeight = FontWeight.SemiBold
                },
                content),
            new Thickness(13));
    }

    private static Control BuildFieldGrid(params Control[] fields)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowSpacing = 11,
            ColumnSpacing = 12
        };

        for (var index = 0; index < fields.Length; index++)
        {
            var row = index / 2;
            if (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            grid.Children.Add(fields[index]
                .Grid_Column(index % 2)
                .Grid_Row(row));
        }

        return grid;
    }

    private static Control BuildField(string label, Control editor)
    {
        return new StackPanel
        {
            Spacing = 5
        }
        .Children(
            new Label
            {
                Content = label,
                Target = editor,
                FontSize = 11,
                FontWeight = FontWeight.Medium,
                Padding = new Thickness(0)
            },
            editor);
    }

    private static Control BuildFooter()
    {
        var cancel = new Button
        {
            Content = AppResources.Get("Common_Cancel"),
            MinWidth = 92
        };
        cancel.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionEditorViewModel.CancelCommand)));

        var save = UiFactory.PrimaryButton(AppResources.Get("Common_Save"));
        save.Bind(
            Button.CommandProperty,
            new Binding(nameof(SubscriptionEditorViewModel.SaveCommand)));

        var error = new Border
        {
            Background = UiPalette.DangerSurface,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 7),
            Child = UiFactory.BoundText(
                nameof(SubscriptionEditorViewModel.ErrorMessage),
                11,
                textWrapping: TextWrapping.Wrap)
        };
        error.Bind(
            IsVisibleProperty,
            new Binding(nameof(SubscriptionEditorViewModel.HasError)));

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
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                RowDefinitions = new RowDefinitions("Auto,Auto"),
                ColumnSpacing = 10,
                RowSpacing = 6
            }
            .Children(
                error,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                }
                .Children(cancel, save)
                .Grid_Column(1),
                progress.Grid_ColumnSpan(2).Grid_Row(1))
        };
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
