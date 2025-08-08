using System;
using System.Reactive.Disposables;

namespace FlockForge;

public partial class MainPage : ContentPage, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;
    private int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object? sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }

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
