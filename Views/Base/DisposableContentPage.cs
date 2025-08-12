using System.Reactive.Disposables;

namespace FlockForge.Views.Base;

public abstract class DisposableContentPage : ContentPage
{
    protected CompositeDisposable Disposables { get; } = new();

    protected override void OnDisappearing()
    {
        Disposables.Clear();
        base.OnDisappearing();
    }

    protected static void StopWebView(WebView? webView)
    {
        if (webView == null)
            return;

        webView.StopLoading();
        webView.Source = null;
    }
}