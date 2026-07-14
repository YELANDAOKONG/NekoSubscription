using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NekoSubscription.ViewModels;

namespace NekoSubscription.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        var vm = new MainViewModel();
        DataContext = vm;

        Title = "NekoSubscription";
        Icon = new WindowIcon(new Bitmap(AssetLoader.Open(new Uri("avares://NekoSubscription/Assets/avalonia-logo.ico"))));

        Content = new TextBox()
            .Text(vm, x => x.Greeting)
            .HorizontalAlignment(HorizontalAlignment.Center)
            .VerticalAlignment(VerticalAlignment.Center);
    }
}