using System.Reactive.Disposables;
#if ANDROID
using AndroidWebView = Android.Webkit.WebView;
#elif IOS
using WebKit;
#endif

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
        if (webView?.Handler?.PlatformView is null)
            return;

#if IOS
        if (webView.Handler.PlatformView is WKWebView wkWebView)
            wkWebView.StopLoading();
#elif ANDROID
        if (webView.Handler.PlatformView is AndroidWebView androidWebView)
            androidWebView.StopLoading();
#endif
        webView.Source = null;
    }
}
