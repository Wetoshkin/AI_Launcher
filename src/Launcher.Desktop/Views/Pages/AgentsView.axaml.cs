using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Launcher.Desktop.Views.Pages;

public partial class AgentsView : UserControl
{
    private INotifyPropertyChanged? _vm;

    public AgentsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = DataContext as INotifyPropertyChanged;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "ServerLog")
        {
            return;
        }

        // Автоскролл консоли к последней строке.
        Dispatcher.UIThread.Post(() =>
        {
            if (this.FindControl<ScrollViewer>("ConsoleScroll") is { } sv)
            {
                sv.Offset = new Vector(sv.Offset.X, sv.Extent.Height);
            }
        }, DispatcherPriority.Background);
    }
}
