using System.Reactive.Disposables;

namespace FlockForge.Views.Base;
public class DisposableContentPage : ContentPage, IDisposable
{
    protected CompositeDisposable Disposables { get; } = new();
    private bool _disposed;

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Disposables.Clear(); // unsubscribe page-level observers
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disposables.Dispose();
        GC.SuppressFinalize(this);
    }
}