using System.Reactive.Disposables;
using Microsoft.Maui.Controls;

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
        webView?.StopLoading();
        if (webView != null)
            webView.Source = null;
    }
}