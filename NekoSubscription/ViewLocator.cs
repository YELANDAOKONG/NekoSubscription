using System;
using System.Diagnostics.CodeAnalysis;

using Avalonia.Controls;
using Avalonia.Controls.Templates;

using NekoSubscription.ViewModels;
using NekoSubscription.Views;

namespace NekoSubscription;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        return param switch
        {
            DashboardViewModel => new DashboardView(),
            SubscriptionsViewModel => new SubscriptionsView(),
            CalendarViewModel => new CalendarView(),
            SettingsViewModel => new SettingsView(),
            SubscriptionEditorViewModel => new SubscriptionEditorView(),
            null => null,
            _ => new TextBlock { Text = $"View not found: {param.GetType().Name}" }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
