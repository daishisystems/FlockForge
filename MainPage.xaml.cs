using System;
using System.Reactive.Disposables;
using Microsoft.Maui.Controls;

namespace FlockForge;

public partial class MainPage : ContentPage, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnProfileTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("/profile");

    private async void OnMyFarmsTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//farms");

    private async void OnGroupsTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("/groups");

    protected override void OnDisappearing()
    {
        _disposables.Clear();
        base.OnDisappearing();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}
