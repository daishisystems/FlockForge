using Microsoft.Maui.Controls;
using FlockForge.ViewModels;

namespace FlockForge.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = _vm = new DashboardViewModel();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        _vm.GridSpan = width > 700 ? 3 : 2; // phones: 2 cols; tablet/landscape: 3 cols
    }
}

