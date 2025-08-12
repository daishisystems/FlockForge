using System.Reactive.Disposables;

namespace FlockForge.Views.Base;

public abstract class DisposableContentPage : ContentPage
{
    protected CompositeDisposable Disposables { get; } = new();
    protected WebView? ActiveWebView { get; set; }

    protected override void OnDisappearing()
    {
        ActiveWebView?.StopLoading();
        if (ActiveWebView != null)
        {
            ActiveWebView.Source = null;
            ActiveWebView = null;
        }

        Disposables.Clear();
        base.OnDisappearing();
    }
}