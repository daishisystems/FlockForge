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

#if IOS
    protected static void StopWebView(WebView? view)
    {
        view?.StopLoading();
        view?.Source = null;
    }
#endif
}